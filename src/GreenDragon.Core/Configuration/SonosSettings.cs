namespace GreenDragon.Core.Configuration;

/// <summary>Controls whether explicit requests can access the home network.</summary>
public sealed class SonosSettings
{
    /// <summary>The configuration section containing Sonos settings.</summary>
    public const string SectionName = "Sonos";
    /// <summary>Allows requested Sonos operations; disabled for the initial scaffold.</summary>
    public bool Enabled { get; set; }

    /// <summary>Delay between completed status requests while the page is visible.</summary>
    public int RefreshIntervalSeconds { get; set; } = BusinessRules.Sonos.RefreshIntervalSeconds;

    /// <summary>Absolute retention of non-catalog metadata, in minutes; at least one hour.</summary>
    public int MetadataCacheDurationMinutes { get; set; } = BusinessRules.Sonos.MetadataCacheMinutes;
}
