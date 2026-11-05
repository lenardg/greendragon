using Aspire.Hosting;

namespace GreenDragon.AppHost;

/// <summary>Orchestrates the portal without starting Sonos network operations.</summary>
public static class AppHost
{
    /// <summary>Starts the local Aspire resource model.</summary>
    /// <param name="args">Arguments forwarded to Aspire.</param>
    /// <returns>A task completed when orchestration stops.</returns>
    public static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        builder.AddProject<Projects.GreenDragon_Web>("greendragon-web")
            .WithHttpHealthCheck("/api/health");
        await builder.Build().RunAsync();
    }
}
