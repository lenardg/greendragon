using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace GreenDragon.ServiceDefaults;

/// <summary>Provides shared telemetry, resilience, service discovery and development health endpoints.</summary>
public static class ServiceDefaultsServiceCollectionExtensions
{
    private const string _healthPath = "/health";
    private const string _alivePath = "/alive";
    private const string _liveTag = "live";
    private const string _otlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string _insightsConnection = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>Registers common host defaults without contacting Sonos.</summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host being configured.</param>
    /// <returns>The builder for further composition.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
        var telemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing.AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments(_healthPath) && !context.Request.Path.StartsWithSegments(_alivePath))
                .AddHttpClientInstrumentation());
        if (!string.IsNullOrWhiteSpace(builder.Configuration[_otlpEndpoint]))
        {
            telemetry.UseOtlpExporter();
        }
        if (!string.IsNullOrWhiteSpace(builder.Configuration[_insightsConnection]))
        {
            telemetry.UseAzureMonitor();
        }
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), [_liveTag]);
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }

    /// <summary>Maps standard development probes; application probes use /api/health.</summary>
    /// <param name="app">The web host.</param>
    /// <returns>The application for further mapping.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(_healthPath);
            app.MapHealthChecks(_alivePath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(_liveTag)
            });
        }
        return app;
    }
}
