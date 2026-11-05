namespace GreenDragon.Core.Sonos;

/// <summary>Contains safe descriptions of failures at the Sonos boundary.</summary>
internal static class SonosMessages
{
    /// <summary>Explains that explicit network access must be enabled before discovery.</summary>
    public const string Disabled = "Sonos access is disabled. Enable Sonos in local configuration first.";

    /// <summary>Describes a discovery failure without exposing upstream exception details.</summary>
    public const string DiscoveryFailed = "Sonos discovery failed. Check the home network and try again.";

    /// <summary>Explains why the worker no longer accepts requests.</summary>
    public const string Stopping = "Sonos access is stopping. Try again after the portal restarts.";
}
