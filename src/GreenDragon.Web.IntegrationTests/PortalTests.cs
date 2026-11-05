using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
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
        await Gateway.Received(1).DiscoverAsync(Arg.Any<CancellationToken>());
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}
