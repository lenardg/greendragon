namespace GreenDragon.Core.Sonos;

/// <summary>Contains safe descriptions of failures at the Sonos boundary.</summary>
internal static class SonosMessages
{
    /// <summary>Explains that a submitted command is invalid.</summary>
    public const string InvalidCommand = "Invalid Sonos command.";
    /// <summary>Requests explicit rediscovery after catalog expiration or device removal.</summary>
    public const string Rediscover = "Speaker information has expired or the player is unavailable. Rediscover speakers and try again.";
    /// <summary>Describes unsupported playback operations safely.</summary>
    public const string Unsupported = "This source does not support the requested operation.";
    /// <summary>Explains an invalid metadata cache retention setting.</summary>
    public const string MetadataRetentionInvalid = "Sonos metadata cache retention must be at least 60 minutes.";
    /// <summary>Explains an invalid browser refresh setting.</summary>
    public const string RefreshIntervalInvalid = "Sonos refresh interval must be between 2 and 60 seconds.";
    /// <summary>Explains that explicit network access must be enabled before discovery.</summary>
    public const string Disabled = "Sonos access is disabled. Enable Sonos in local configuration first.";

    /// <summary>Describes a discovery failure without exposing upstream exception details.</summary>
    public const string DiscoveryFailed = "Sonos discovery failed. Check the home network and try again.";

    /// <summary>Explains why the worker no longer accepts requests.</summary>
    public const string Stopping = "Sonos access is stopping. Try again after the portal restarts.";
}
