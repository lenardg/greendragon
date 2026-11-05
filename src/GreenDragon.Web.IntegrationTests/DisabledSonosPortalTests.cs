using NSubstitute;
using Shouldly;

namespace GreenDragon.Web.IntegrationTests;

public sealed class DisabledSonosPortalTests : PortalTestBase
{
    protected override bool SonosEnabled => false;

    [Fact]
    public async Task DisabledHomeDoesNotOfferOrAutomaticallyLoadSpeakers()
    {
        var html = await Client.GetStringAsync("/", TestContext.Current.CancellationToken);
        html.ShouldContain("Sonos is disabled.");
        html.ShouldNotContain("data-discovery-form");
        html.ShouldNotContain("data-auto-load=\"true\"");
        (await Client.GetAsync("/api/health", TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await Gateway.DidNotReceiveWithAnyArgs().DiscoverAsync(TestContext.Current.CancellationToken);
    }
}
