namespace GreenDragon.Core.Sonos;

/// <summary>Describes a discovered room without exposing upstream objects to pages.</summary>
/// <param name="Id">The stable Sonos player identifier.</param>
/// <param name="RoomName">The room name configured in Sonos.</param>
/// <param name="ModelName">The speaker model reported by the device.</param>
public sealed record SpeakerSnapshot(string Id, string RoomName, string ModelName);
