using GreenDragon.Core.BusinessLogic;
using GreenDragon.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GreenDragon.Core.Sonos;

/// <summary>Registers Core logic and its embedded Sonos command processor.</summary>
public static class SonosServiceCollectionExtensions
{
    /// <summary>Registers one worker instance shared by host and portal requests.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Configuration containing the Sonos section.</param>
    /// <returns>The collection for further composition.</returns>
    public static IServiceCollection AddSonos(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SonosSettings>().Bind(configuration.GetSection(SonosSettings.SectionName))
            .Validate(settings => settings.MetadataCacheDurationMinutes >= BusinessRules.Sonos.MetadataCacheMinutes,
                SonosMessages.MetadataRetentionInvalid)
            .Validate(settings => settings.RefreshIntervalSeconds is >= BusinessRules.Sonos.MinimumRefreshSeconds
                and <= BusinessRules.Sonos.MaximumRefreshSeconds, SonosMessages.RefreshIntervalInvalid)
            .ValidateOnStart();
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ISonosMetadataCache, SonosMetadataCache>();
        services.AddSingleton<ISonosGateway, OpenPhonosGateway>();
        services.AddSingleton<SonosBackgroundService>();
        services.AddHostedService(provider => provider.GetRequiredService<SonosBackgroundService>());
        services.AddSingleton<ISonosBL, SonosBL>();
        services.AddSingleton<IExampleBL, ExampleBL>();
        return services;
    }
}
