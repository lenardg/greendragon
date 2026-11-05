namespace GreenDragon.Core.Sonos;

/// <summary>Describes a discovered room without exposing upstream objects to pages.</summary>
/// <param name="Id">The stable Sonos player identifier.</param>
/// <param name="RoomName">The room name configured in Sonos.</param>
/// <param name="ModelName">The speaker model reported by the device.</param>
public sealed record SpeakerSnapshot(string Id, string RoomName, string ModelName)
{
    /// <summary>The shared playback information from this room's group coordinator.</summary>
    public SpeakerPlaybackSnapshot Playback { get; init; } = new();

    /// <summary>The room's volume percentage, or null when unavailable.</summary>
    public int? Volume { get; init; }

    /// <summary>The room's mute setting, or null when unavailable.</summary>
    public bool? IsMuted { get; init; }
}
