using GreenDragon.Core.Configuration;
using GreenDragon.Core.Sonos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace GreenDragon.Core.UnitTests;

public sealed class SonosBackgroundServiceTests
{
    [Fact]
    public async Task DisabledWorkerNeverCallsGateway()
    {
        var gateway = Substitute.For<ISonosGateway>();
        using var worker = CreateWorker(gateway, enabled: false);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await Should.ThrowAsync<BusinessLogicException>(() => worker.DiscoverAsync(TestContext.Current.CancellationToken));
        await worker.StopAsync(TestContext.Current.CancellationToken);
        await gateway.DidNotReceiveWithAnyArgs().DiscoverAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecutesRequestsSequentiallyAndKeepsWorkingAfterFailure()
    {
        var gateway = Substitute.For<ISonosGateway>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            entered.TrySetResult();
            return release.Task;
        }, _ => throw new InvalidOperationException("Mock failure"),
        _ => Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([new("test", "Bedroom", "Mock speaker")]));
        using var worker = CreateWorker(gateway);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var first = worker.DiscoverAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var second = worker.DiscoverAsync(TestContext.Current.CancellationToken);
        await gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>());
        release.SetResult([]);
        (await first).ShouldBeEmpty();
        await Should.ThrowAsync<BusinessLogicException>(() => second);
        var third = await worker.DiscoverAsync(TestContext.Current.CancellationToken);
        third.Single().RoomName.ShouldBe("Bedroom");
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CanceledQueuedRequestNeverReachesGateway()
    {
        var gateway = Substitute.For<ISonosGateway>();
        using var worker = CreateWorker(gateway);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await Should.ThrowAsync<OperationCanceledException>(() => worker.DiscoverAsync(cancellation.Token));
        await worker.StopAsync(TestContext.Current.CancellationToken);
        await gateway.DidNotReceiveWithAnyArgs().DiscoverAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CancelingAnInFlightCallerDoesNotOverlapTheNextRequest()
    {
        var gateway = Substitute.For<ISonosGateway>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            entered.TrySetResult();
            return release.Task;
        }, _ => Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([]));
        using var worker = CreateWorker(gateway);
        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var first = worker.DiscoverAsync(cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => first);
        var second = worker.DiscoverAsync(TestContext.Current.CancellationToken);
        await gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>());
        release.SetResult([]);
        (await second).ShouldBeEmpty();
        await worker.StopAsync(TestContext.Current.CancellationToken);
        await gateway.Received(2).DiscoverAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShutdownRejectsWaitingWritersWhileAwaitingUncancelableDiscovery()
    {
        var gateway = Substitute.For<ISonosGateway>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            entered.TrySetResult();
            return release.Task;
        });
        using var worker = CreateWorker(gateway);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var active = worker.DiscoverAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var queued = Enumerable.Range(0, BusinessRules.SonosQueueCapacity)
            .Select(_ => worker.DiscoverAsync(TestContext.Current.CancellationToken)).ToArray();
        var waitingWriter = worker.DiscoverAsync(TestContext.Current.CancellationToken);
        waitingWriter.IsCompleted.ShouldBeFalse();
        var stopping = worker.StopAsync(TestContext.Current.CancellationToken);
        await Should.ThrowAsync<BusinessLogicException>(() => waitingWriter);
        await Should.ThrowAsync<BusinessLogicException>(() => worker.DiscoverAsync(TestContext.Current.CancellationToken));
        stopping.IsCompleted.ShouldBeFalse();
        release.SetResult([]);
        await Should.ThrowAsync<OperationCanceledException>(() => active);
        await stopping;
        foreach (var request in queued)
        {
            await Should.ThrowAsync<OperationCanceledException>(() => request);
        }
        await gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MixedCommandsAndReadsRemainSerializedAfterCallerCancellation()
    {
        var gateway = Substitute.For<ISonosGateway>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.RefreshAsync(Arg.Any<CancellationToken>()).Returns(_ => { entered.TrySetResult(); return release.Task; });
        var command = new SonosCommand("known", SonosCommandKind.Next);
        gateway.ExecuteAsync(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([]));
        using var worker = CreateWorker(gateway);
        using var canceled = new CancellationTokenSource();
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var first = worker.RefreshAsync(canceled.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        canceled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => first);
        var next = worker.ExecuteCommandAsync(command, TestContext.Current.CancellationToken);
        await gateway.DidNotReceive().ExecuteAsync(command, Arg.Any<CancellationToken>());
        release.SetResult([]);
        (await next).ShouldBeEmpty();
        await gateway.Received(1).ExecuteAsync(command, Arg.Any<CancellationToken>());
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DisabledWorkerRejectsReadsAndCommandsAndInvalidCommandsNeverReachGateway()
    {
        var gateway = Substitute.For<ISonosGateway>();
        using var worker = CreateWorker(gateway, enabled: false);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await Should.ThrowAsync<BusinessLogicException>(() => worker.RefreshAsync(TestContext.Current.CancellationToken));
        await Should.ThrowAsync<BusinessLogicException>(() => worker.ExecuteCommandAsync(new("known", SonosCommandKind.Next), TestContext.Current.CancellationToken));
        Should.Throw<BusinessLogicException>(() => worker.ExecuteCommandAsync(new("known", SonosCommandKind.SetVolume, 101), TestContext.Current.CancellationToken));
        gateway.ReceivedCalls().ShouldBeEmpty();
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }
    private static SonosBackgroundService CreateWorker(ISonosGateway gateway, bool enabled = true)
    {
        return new SonosBackgroundService(gateway, Options.Create(new SonosSettings { Enabled = enabled }),
            NullLogger<SonosBackgroundService>.Instance, NullLoggerFactory.Instance);
    }
}
