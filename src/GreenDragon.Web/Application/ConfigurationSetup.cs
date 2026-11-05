using GreenDragon.Web.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace GreenDragon.Web.Application;

/// <summary>Loads local overrides before secrets, environment variables and command-line arguments.</summary>
public static class ConfigurationSetup
{
    /// <summary>Inserts local settings into the default configuration precedence.</summary>
    /// <param name="builder">The web host being composed.</param>
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        var source = new JsonConfigurationSource
        {
            Path = HostingSettings.LocalSettingsFile,
            Optional = true,
            ReloadOnChange = false,
            FileProvider = builder.Environment.ContentRootFileProvider
        };
        var sources = builder.Configuration.Sources;
        var environmentSettings = sources.OfType<JsonConfigurationSource>()
            .Last(item => item.Path == $"appsettings.{builder.Environment.EnvironmentName}.json");
        sources.Insert(sources.IndexOf(environmentSettings) + 1, source);
        builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection(FeatureFlags.SectionName));
    }
}
