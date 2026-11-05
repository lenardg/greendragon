namespace GreenDragon.Core.Configuration;

/// <summary>Controls whether explicit requests can access the home network.</summary>
public sealed class SonosSettings
{
    /// <summary>The configuration section containing Sonos settings.</summary>
    public const string SectionName = "Sonos";
    /// <summary>Allows requested Sonos operations; disabled for the initial scaffold.</summary>
    public bool Enabled { get; set; }
}
