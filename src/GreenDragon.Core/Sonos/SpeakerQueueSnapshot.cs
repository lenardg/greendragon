namespace GreenDragon.Core.Sonos;

/// <summary>A bounded, fresh preview of the active source's queue.</summary>
public sealed record SpeakerQueueSnapshot
{
    /// <summary>Whether queue information could be read successfully.</summary>
    public bool IsAvailable { get; init; }
    /// <summary>Whether the current source uses the Sonos queue rather than radio or line-in.</summary>
    public bool IsActive { get; init; }
    /// <summary>The total number of tracks in the queue.</summary>
    public uint TotalCount { get; init; }
    /// <summary>The current track's one-based queue position, or zero when unknown.</summary>
    public uint CurrentTrackNumber { get; init; }
    /// <summary>Queue entries starting at the current position, bounded by the preview limit.</summary>
    public IReadOnlyList<SpeakerQueueTrack> Tracks { get; init; } = [];
    /// <summary>The first queue entry, used when repeat-all wraps around.</summary>
    public SpeakerQueueTrack? FirstTrack { get; init; }
    /// <summary>The next track when the source and playback mode determine it.</summary>
    public SpeakerQueueTrack? NextTrack { get; init; }
}
