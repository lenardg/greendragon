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
}
