using System.Net;
using System.Text.RegularExpressions;
using GreenDragon.Core;
using GreenDragon.Core.Configuration;
using GreenDragon.Core.Sonos;
using GreenDragon.Web.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace GreenDragon.Web.IntegrationTests;

public sealed partial class SpeakerControlsTests : PortalTestBase
{
    [Theory]
    [InlineData("Previous")]
    [InlineData("TogglePlayback")]
    [InlineData("Next")]
    [InlineData("CycleRepeat")]
    [InlineData("ToggleShuffle")]
    [InlineData("VolumeDown")]
    [InlineData("VolumeUp")]
    [InlineData("SetVolume")]
    public async Task ValidCommandsReachMockAndReturnCards(string kind)
    {
        Gateway.ExecuteAsync(Arg.Any<SonosCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Snapshots()));
        var result = await PostAsync("Command", new() { ["SpeakerId"] = "known", ["Kind"] = kind, ["Volume"] = "42" });
        result.ShouldContain("data-operation-succeeded=\"true\"");
        result.ShouldContain("42%");
        result.ShouldContain("Repeat: One");
        result.ShouldContain("Shuffle: On");
        result.ShouldNotContain("SHOULD-NOT-SHOW-MODEL");
        result.IndexOf("Track title", StringComparison.Ordinal).ShouldBeLessThan(result.IndexOf("class=\"cover-art\"", StringComparison.Ordinal));
        await Gateway.Received(1).ExecuteAsync(Arg.Is<SonosCommand>(command => command.SpeakerId == "known"
            && command.Kind == Enum.Parse<SonosCommandKind>(kind)
            && command.Volume == (kind == "SetVolume" ? 42 : null)), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "Next", "42")]
    [InlineData("known", "999", "42")]
    [InlineData("known", "None", "42")]
    [InlineData("known", "SetVolume", "-1")]
    [InlineData("known", "SetVolume", "101")]
    [InlineData("known", "SetVolume", "invalid")]
    [InlineData("known", "SetVolume", "")]
    public async Task InvalidCommandsNeverReachGateway(string id, string kind, string volume)
    {
        var result = await PostAsync("Command", new() { ["SpeakerId"] = id, ["Kind"] = kind, ["Volume"] = volume });
        result.ShouldContain("data-operation-succeeded=\"false\"");
        await Gateway.DidNotReceive().ExecuteAsync(Arg.Any<SonosCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Command")]
    [InlineData("Refresh")]
    public async Task AllNewHandlersEnforceAntiforgery(string handler)
    {
        var response = await Client.PostAsync($"/?handler={handler}", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        gatewayCallsShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshReadsFreshStateWithoutDiscoveryAndReturnsFragment()
    {
        Gateway.RefreshAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Snapshots()));
        var first = await PostAsync("Refresh", []);
        first.ShouldContain("data-speaker-id=\"known\"");
        first.ShouldNotContain("<!DOCTYPE");
        await Gateway.Received(1).RefreshAsync(Arg.Any<CancellationToken>());
        await Gateway.DidNotReceive().DiscoverAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task NativeFormsRenderTheFullPageAndExplicitRediscoveryIsPassedThrough()
    {
        Gateway.DiscoverAsync(Arg.Any<CancellationToken>(), true).Returns(Task.FromResult(Snapshots()));
        var result = await PostAsync("Discover", new() { ["rediscover"] = "true" }, fragment: false);
        result.ShouldContain("<!DOCTYPE html>");
        result.ShouldContain("value=\"SetVolume\"");
        result.ShouldContain("name=\"Volume\"");
        result.ShouldContain("<svg");
        await Gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>(), true);
    }

    [Fact]
    public async Task FailedCommandIsRetryableAndDoesNotTriggerAnotherCommandOrDiscovery()
    {
        Gateway.ExecuteAsync(Arg.Any<SonosCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new BusinessLogicException("Device unavailable"), _ => Task.FromResult(Snapshots()));
        var fields = new Dictionary<string, string> { ["SpeakerId"] = "known", ["Kind"] = "Next" };
        (await PostAsync("Command", fields)).ShouldContain("data-operation-succeeded=\"false\"");
        await Gateway.Received(1).ExecuteAsync(Arg.Any<SonosCommand>(), Arg.Any<CancellationToken>());
        (await PostAsync("Command", fields)).ShouldContain("data-operation-succeeded=\"true\"");
        await Gateway.Received(2).ExecuteAsync(Arg.Any<SonosCommand>(), Arg.Any<CancellationToken>());
        await Gateway.DidNotReceive().DiscoverAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task DisabledConfigurationRejectsValidCommandsAndRefreshes()
    {
        var token = await TokenAsync();
        Factory.Services.GetRequiredService<IOptions<SonosSettings>>().Value.Enabled = false;
        (await PostAsync("Command", new() { ["SpeakerId"] = "known", ["Kind"] = "Next" }, token: token))
            .ShouldContain("data-operation-succeeded=\"false\"");
        (await PostAsync("Refresh", [], token: token)).ShouldContain("data-operation-succeeded=\"false\"");
        gatewayCallsShouldBeEmpty();
    }

    private void gatewayCallsShouldBeEmpty()
    {
        Gateway.ReceivedCalls().ShouldBeEmpty();
    }

    private static IReadOnlyList<SpeakerSnapshot> Snapshots()
    {
        return [new("known", "Room", "SHOULD-NOT-SHOW-MODEL")
        {
            Volume = 42, IsMuted = false,
            Playback = new SpeakerPlaybackSnapshot
            {
                Title = "Track title", State = SpeakerPlaybackState.Playing,
                Position = TimeSpan.FromSeconds(10), Duration = TimeSpan.FromMinutes(3),
                Repeat = SonosRepeatMode.One, Shuffle = true, CanPrevious = true, CanNext = true, CanTogglePlayback = true
            }
        }];
    }

    private async Task<string> TokenAsync()
    {
        var html = await Client.GetStringAsync("/", TestContext.Current.CancellationToken);
        return WebUtility.HtmlDecode(TokenPattern().Match(html).Groups[1].Value);
    }

    private async Task<string> PostAsync(string handler, Dictionary<string, string> fields, bool fragment = true, string? token = null)
    {
        fields = new(fields) { ["__RequestVerificationToken"] = token ?? await TokenAsync() };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/?handler={handler}") { Content = new FormUrlEncodedContent(fields) };
        if (fragment)
        {
            request.Headers.Add(PortalRequests.RequestedWith, PortalRequests.Fetch);
        }

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.ShouldBeTrue();
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
