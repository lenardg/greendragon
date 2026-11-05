using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GreenDragon.Core;
using GreenDragon.Core.Sonos;
using GreenDragon.Web.Application;
using NSubstitute;
using Shouldly;

namespace GreenDragon.Web.IntegrationTests;

public sealed partial class PortalTests : PortalTestBase
{
    [Fact]
    public async Task HomeAndHealthDoNotContactSonos()
    {
        var response = await Client.GetAsync("/", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.ShouldContain("The Green Dragon");
        html.ShouldContain("data-auto-load=\"true\"");
        html.ShouldContain("data-loading-card");
        html.IndexOf("data-speaker-results", StringComparison.Ordinal).ShouldBeLessThan(html.IndexOf("data-discovery-form", StringComparison.Ordinal));
        response.Headers.GetValues(WebSecurityHeaders.ContentSecurityPolicy).Single().ShouldBe(WebSecurityHeaders.Policy);
        response.Headers.GetValues(WebSecurityHeaders.ContentTypeOptions).Single().ShouldBe(WebSecurityHeaders.NoSniff);
        response.Headers.GetValues(WebSecurityHeaders.ReferrerPolicy).Single().ShouldBe(WebSecurityHeaders.ReferrerPolicyValue);
        (await Client.GetAsync("/api/health", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var version = await Client.GetFromJsonAsync<string>("/api/health?version=true", TestContext.Current.CancellationToken);
        Version.TryParse(version, out _).ShouldBeTrue();
        await Gateway.DidNotReceiveWithAnyArgs().DiscoverAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DiscoveryWithoutAntiforgeryIsRejectedBeforeCallingSonos()
    {
        var response = await Client.PostAsync("/?handler=Discover", new FormUrlEncodedContent([]),
            TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await Gateway.DidNotReceiveWithAnyArgs().DiscoverAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ValidDiscoveryPostReachesMockAndEncodesRoomNames()
    {
        var html = await Client.GetStringAsync("/", TestContext.Current.CancellationToken);
        var match = AntiforgeryToken().Match(html);
        match.Success.ShouldBeTrue();
        var token = WebUtility.HtmlDecode(match.Groups[1].Value);
        var response = await Client.PostAsync("/?handler=Discover",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.ShouldContain("&lt;Bedroom&gt;");
        result.ShouldNotContain("<Bedroom>");
        result.ShouldContain("data-auto-load=\"false\"");
        await Gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OverviewShowsAllRoomsAndEncodesPlaybackMetadata()
    {
        const string art = "data:image/png;base64,iVBORw0KGgo=";
        Gateway.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SpeakerSnapshot>>(
        [
            new("bedroom", "Bedroom", "Sonos One")
            {
                Playback = new SpeakerPlaybackSnapshot
                {
                    State = SpeakerPlaybackState.Playing,
                    Title = "<script>track</script>", Artist = "<Artist>", Album = "<Album>",
                    CoverArtDataUrl = art, Position = TimeSpan.FromSeconds(83), Duration = TimeSpan.FromSeconds(245)
                },
                Volume = 35, IsMuted = true
            },
            new("kitchen", "Kitchen", "Sonos Era")
        ]));
        var result = await DiscoverAsync();
        result.ShouldContain("Bedroom");
        result.ShouldContain("Kitchen");
        result.ShouldContain(art);
        result.ShouldContain("Playing");
        result.ShouldContain("00:01:23");
        result.ShouldContain("00:04:05");
        result.ShouldContain("35%");
        result.ShouldContain("Muted");
        result.ShouldContain("&lt;script&gt;track&lt;/script&gt;");
        result.ShouldContain("&lt;Artist&gt;");
        result.ShouldContain("&lt;Album&gt;");
        result.ShouldNotContain("<script>track</script>");
        result.ShouldContain("No cover art available");
        result.ShouldContain("Playback unavailable");
        result.ShouldContain("Unavailable");
        await Gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedOverviewAllowsRetryAndAnEmptyResult()
    {
        Gateway.DiscoverAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new BusinessLogicException("Mock failure"),
                _ => Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([]));
        var result = await DiscoverAsync();
        result.ShouldContain("The speaker request could not be completed.");
        result.ShouldContain("Refresh speakers");
        result.ShouldContain("data-auto-load=\"false\"");
        (await DiscoverAsync()).ShouldContain("No speakers found.");
        await Gateway.Received(2).DiscoverAsync(Arg.Any<CancellationToken>());
    }

    private async Task<string> DiscoverAsync()
    {
        var html = await Client.GetStringAsync("/", TestContext.Current.CancellationToken);
        var token = WebUtility.HtmlDecode(AntiforgeryToken().Match(html).Groups[1].Value);
        var response = await Client.PostAsync("/?handler=Discover",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}
