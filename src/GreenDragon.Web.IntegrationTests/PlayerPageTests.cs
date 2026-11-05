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

public sealed partial class PlayerPageTests : PortalTestBase
{
    private const string _playerUrl = "/players/known";

    [Fact]
    public async Task GetRendersShellWithoutAnySonosCallAndDisabledShellCannotAutoLoad()
    {
        var html = await Client.GetStringAsync(_playerUrl, TestContext.Current.CancellationToken);
        html.ShouldContain("data-auto-load=\"true\"");
        html.ShouldContain("All players");
        Gateway.ReceivedCalls().ShouldBeEmpty();
        Factory.Services.GetRequiredService<IOptions<SonosSettings>>().Value.Enabled = false;
        html = await Client.GetStringAsync(_playerUrl, TestContext.Current.CancellationToken);
        html.ShouldContain("Sonos is disabled");
        html.ShouldNotContain("data-discovery-form");
        Gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshRendersRoomQueueAndEncodedMetadataUsingPlayerSpecificForms()
    {
        Gateway.ReadPlayerAsync("known", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Details()));
        var html = await PostAsync("Refresh", []);
        html.ShouldNotContain("<!DOCTYPE");
        html.ShouldContain("class=\"rooms player-room\"");
        html.ShouldContain("&lt;Room&gt;");
        html.ShouldContain("&lt;Current&gt;");
        html.ShouldContain("&lt;Next&gt;");
        html.ShouldNotContain("<Next>");
        html.ShouldContain("Up next");
        html.ShouldContain("aria-current=\"true\"");
        html.ShouldContain("action=\"/players/known?handler=Command\"");
        await Gateway.Received(1).ReadPlayerAsync("known", Arg.Any<CancellationToken>());
        await Gateway.DidNotReceive().DiscoverAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Theory]
    [InlineData("Refresh")]
    [InlineData("Command")]
    public async Task PostsRequireAntiforgery(string handler)
    {
        var response = await Client.PostAsync($"{_playerUrl}?handler={handler}", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("other", "Next", "42")]
    [InlineData("known", "SetVolume", "101")]
    [InlineData("known", "SetVolume", "invalid")]
    [InlineData("known", "999", "42")]
    [InlineData("known", "None", "42")]
    public async Task InvalidOrMismatchedCommandsNeverReachGateway(string id, string kind, string volume)
    {
        var html = await PostAsync("Command", new() { ["SpeakerId"] = id, ["Kind"] = kind, ["Volume"] = volume });
        html.ShouldContain("data-operation-succeeded=\"false\"");
        Gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task NativeCommandStaysOnPlayerPageAndReloadsItsQueue()
    {
        Gateway.ExecuteAsync(Arg.Any<SonosCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([]));
        Gateway.ReadPlayerAsync("known", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Details()));
        var html = await PostAsync("Command", new() { ["SpeakerId"] = "known", ["Kind"] = "SetVolume", ["Volume"] = "42" }, fragment: false);
        html.ShouldContain("<!DOCTYPE html>");
        html.ShouldContain("data-player-queue");
        html.ShouldContain("action=\"/players/known?handler=Command\"");
        await Gateway.Received(1).ExecuteAsync(Arg.Is<SonosCommand>(command => command.SpeakerId == "known" && command.Volume == 42), Arg.Any<CancellationToken>());
        await Gateway.Received(1).ReadPlayerAsync("known", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadFailureAllowsExplicitRetryWithoutDiscovery()
    {
        Gateway.ReadPlayerAsync("known", Arg.Any<CancellationToken>()).Returns(_ => throw new BusinessLogicException("Missing player"), _ => Task.FromResult(Details()));
        (await PostAsync("Refresh", [], fragment: false)).ShouldContain("Refresh player");
        (await PostAsync("Refresh", [])).ShouldContain("data-operation-succeeded=\"true\"");
        await Gateway.Received(2).ReadPlayerAsync("known", Arg.Any<CancellationToken>());
        await Gateway.DidNotReceive().DiscoverAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }

    [Theory]
    [InlineData(false, false, "Refresh player to try loading the queue again.")]
    [InlineData(true, false, "This source does not use the Sonos queue.")]
    [InlineData(true, true, "The queue is empty.")]
    public async Task QueueStatesKeepThePlayerControlsAvailable(bool available, bool active, string message)
    {
        var details = Details() with { Queue = new SpeakerQueueSnapshot { IsAvailable = available, IsActive = active } };
        Gateway.ReadPlayerAsync("known", Arg.Any<CancellationToken>()).Returns(Task.FromResult(details));
        var html = await PostAsync("Refresh", []);
        html.ShouldContain(message);
        html.ShouldContain("data-command-form");
    }

    [Fact]
    public async Task DisabledPostsNeverReachGateway()
    {
        var token = await TokenAsync();
        Factory.Services.GetRequiredService<IOptions<SonosSettings>>().Value.Enabled = false;
        (await PostAsync("Refresh", [], token: token)).ShouldContain("data-operation-succeeded=\"false\"");
        (await PostAsync("Command", new() { ["SpeakerId"] = "known", ["Kind"] = "Next" }, token: token)).ShouldContain("data-operation-succeeded=\"false\"");
        Gateway.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task OverviewLinksRoomDisplayToDedicatedPage()
    {
        Gateway.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([Details().Speaker]));
        var home = await Client.GetStringAsync("/", TestContext.Current.CancellationToken);
        var token = WebUtility.HtmlDecode(TokenPattern().Match(home).Groups[1].Value);
        var response = await Client.PostAsync("/?handler=Discover", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }), TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.ShouldContain("class=\"player-link\" href=\"/players/known\"");
        html.ShouldContain("action=\"/?handler=Command\"");
    }

    private static SpeakerDetailsSnapshot Details()
    {
        return new(new SpeakerSnapshot("known", "<Room>", "Mock")
        {
            Volume = 42,
            Playback = new SpeakerPlaybackSnapshot { Title = "<Current>", Artist = "Artist", Shuffle = false, Repeat = SonosRepeatMode.Off }
        }, new SpeakerQueueSnapshot
        {
            IsAvailable = true,
            IsActive = true,
            TotalCount = 2,
            CurrentTrackNumber = 1,
            Tracks = [new(1, "<Current>", "Artist", "Album"), new(2, "<Next>", "Next artist", "Album")]
        });
    }

    private async Task<string> TokenAsync()
    {
        var html = await Client.GetStringAsync(_playerUrl, TestContext.Current.CancellationToken);
        return WebUtility.HtmlDecode(TokenPattern().Match(html).Groups[1].Value);
    }

    private async Task<string> PostAsync(string handler, Dictionary<string, string> fields, bool fragment = true, string? token = null)
    {
        fields = new(fields) { ["__RequestVerificationToken"] = token ?? await TokenAsync() };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_playerUrl}?handler={handler}") { Content = new FormUrlEncodedContent(fields) };
        if (fragment)
        {
            request.Headers.Add(PortalRequests.RequestedWith, PortalRequests.Fetch);
        }

        using var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.ShouldBeTrue();
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
