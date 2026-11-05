using GreenDragon.Core.Sonos;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace GreenDragon.Web.IntegrationTests;

public abstract class PortalTestBase : IAsyncLifetime
{
    protected virtual bool SonosEnabled => true;
    protected ISonosGateway Gateway { get; } = Substitute.For<ISonosGateway>();
    protected WebApplicationFactory<WebProgram> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        Gateway.DiscoverAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SpeakerSnapshot>>([new("mock-room", "<Bedroom>", "Mock speaker")]));
        Factory = new WebApplicationFactory<WebProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Sonos:Enabled"] = SonosEnabled.ToString(),
                    ["ConnectionStrings:GreenDragon-keyvault"] = null,
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = null,
                    ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = null
                }));
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<ISonosGateway>();
                services.AddSingleton(Gateway);
            });
        });
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }
}
