namespace GreenDragon.Core.Sonos;

/// <summary>Transport states displayed by the speaker overview.</summary>
public enum SpeakerPlaybackState
{
    /// <summary>The player did not report its state.</summary>
    Unknown,
    /// <summary>The group is playing audio.</summary>
    Playing,
    /// <summary>The group is paused.</summary>
    Paused,
    /// <summary>The group is stopped.</summary>
    Stopped,
    /// <summary>The group is changing tracks or buffering.</summary>
    Transitioning,
    /// <summary>The group has no media loaded.</summary>
    NoMedia
}
