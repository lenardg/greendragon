using Serilog;

namespace GreenDragon.Web.Application;

/// <summary>Routes structured console logs to the configured telemetry providers.</summary>
public static class LoggingSetup
{
    /// <summary>Registers Serilog without a global bootstrap logger.</summary>
    /// <param name="builder">The web host being composed.</param>
    public static void AddApplicationLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext(), writeToProviders: true);
    }
}
