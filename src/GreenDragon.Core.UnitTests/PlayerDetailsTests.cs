using GreenDragon.Core.BusinessLogic;
using GreenDragon.Core.Configuration;
using GreenDragon.Core.Sonos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace GreenDragon.Core.UnitTests;

public sealed class PlayerDetailsTests
{
    [Theory]
    [InlineData(SonosRepeatMode.Off, false, 1, 2)]
    [InlineData(SonosRepeatMode.One, false, 1, 1)]
    [InlineData(SonosRepeatMode.One, true, 1, 1)]
    [InlineData(SonosRepeatMode.All, false, 2, 1)]
    [InlineData(SonosRepeatMode.Off, false, 2, 0)]
    [InlineData(SonosRepeatMode.Off, true, 1, 0)]
    public async Task NextTrackRespectsRepeatShuffleAndEndOfQueue(SonosRepeatMode repeat, bool shuffle, uint current, uint expected)
    {
        var gateway = Substitute.For<ISonosGateway>();
        var first = new SpeakerQueueTrack(1, "First", null, null);
        gateway.ReadPlayerAsync("known", Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SpeakerDetailsSnapshot(
            new SpeakerSnapshot("known", "Room", "Mock") { Playback = new SpeakerPlaybackSnapshot { Repeat = repeat, Shuffle = shuffle } },
            new SpeakerQueueSnapshot
            {
                IsAvailable = true,
                IsActive = true,
                TotalCount = 2,
                CurrentTrackNumber = current,
                FirstTrack = first,
                Tracks = current == 1 ? [first, new(2, "Second", null, null)] : [new(2, "Second", null, null)]
            })));
        var options = Options.Create(new SonosSettings { Enabled = true });
        using var worker = new SonosBackgroundService(gateway, options, NullLogger<SonosBackgroundService>.Instance, NullLoggerFactory.Instance);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var result = await new SonosBL(worker, options).ReadPlayerAsync("known", TestContext.Current.CancellationToken);
        (result.Queue.NextTrack?.Number ?? 0).ShouldBe(expected);
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PlayerReadsWaitBehindUncancelableDiscoveryAndInvalidIdsAreRejected()
    {
        var gateway = Substitute.For<ISonosGateway>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(_ => { entered.SetResult(); return release.Task; });
        gateway.ReadPlayerAsync("known", Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SpeakerDetailsSnapshot(new("known", "Room", "Mock"), new())));
        using var worker = new SonosBackgroundService(gateway, Options.Create(new SonosSettings { Enabled = true }), NullLogger<SonosBackgroundService>.Instance, NullLoggerFactory.Instance);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var discovery = worker.DiscoverAsync(cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => discovery);
        var details = worker.ReadPlayerAsync("known", TestContext.Current.CancellationToken);
        await gateway.DidNotReceive().ReadPlayerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        release.SetResult([]);
        (await details).Speaker.Id.ShouldBe("known");
        Should.Throw<BusinessLogicException>(() => worker.ReadPlayerAsync(" ", TestContext.Current.CancellationToken));
        Should.Throw<BusinessLogicException>(() => worker.ReadPlayerAsync(new string('x', BusinessRules.Sonos.MaximumIdLength + 1), TestContext.Current.CancellationToken));
        await gateway.Received(1).ReadPlayerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }
}
