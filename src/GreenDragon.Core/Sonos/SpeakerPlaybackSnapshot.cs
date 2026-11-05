namespace GreenDragon.Core.Sonos;

/// <summary>A point-in-time view of a group's current track and transport.</summary>
public sealed record SpeakerPlaybackSnapshot
{
    /// <summary>The normalized transport state.</summary>
    public SpeakerPlaybackState State { get; init; }
    /// <summary>The track or source title, when supplied.</summary>
    public string? Title { get; init; }
    /// <summary>The artist, when supplied.</summary>
    public string? Artist { get; init; }
    /// <summary>The album, when supplied.</summary>
    public string? Album { get; init; }
    /// <summary>Validated raster artwork embedded for display over HTTPS.</summary>
    public string? CoverArtDataUrl { get; init; }
    /// <summary>The reported elapsed time, or null for unsupported sources.</summary>
    public TimeSpan? Position { get; init; }
    /// <summary>The reported duration, or null for streams and unsupported sources.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>The reported repeat mode, or null when unavailable.</summary>
    public SonosRepeatMode? Repeat { get; init; }
    /// <summary>The reported shuffle setting, or null when unavailable.</summary>
    public bool? Shuffle { get; init; }
    /// <summary>Whether the current source allows previous-track navigation.</summary>
    public bool CanPrevious { get; init; }
    /// <summary>Whether the current source allows the displayed play/pause action.</summary>
    public bool CanTogglePlayback { get; init; }
    /// <summary>Whether the current source allows next-track navigation.</summary>
    public bool CanNext { get; init; }
}
