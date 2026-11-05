namespace GreenDragon.Core.Sonos;

/// <summary>The selected room's controls and its coordinator's queue.</summary>
/// <param name="Speaker">The room's current playback and volume.</param>
/// <param name="Queue">The current source's queue information.</param>
public sealed record SpeakerDetailsSnapshot(SpeakerSnapshot Speaker, SpeakerQueueSnapshot Queue);
