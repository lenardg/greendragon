namespace GreenDragon.Core.Sonos;

/// <summary>A track in the coordinator's queue, with its one-based position.</summary>
/// <param name="Number">The position in queue order.</param>
/// <param name="Title">The reported track title.</param>
/// <param name="Artist">The reported artist, when available.</param>
/// <param name="Album">The reported album, when available.</param>
public sealed record SpeakerQueueTrack(uint Number, string? Title, string? Artist, string? Album);
