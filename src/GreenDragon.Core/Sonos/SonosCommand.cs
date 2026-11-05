namespace GreenDragon.Core.Sonos;

/// <summary>A validated request addressed to a discovered player ID, never a device URL.</summary>
/// <param name="SpeakerId">The discovered stable player ID.</param>
/// <param name="Kind">The requested operation.</param>
/// <param name="Volume">The target percentage for SetVolume only.</param>
public sealed record SonosCommand(string SpeakerId, SonosCommandKind Kind, int? Volume = null)
{
    /// <summary>Rejects invalid commands before queuing network work.</summary>
    /// <exception cref="BusinessLogicException">The identifier, kind or volume is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SpeakerId) || SpeakerId.Length > BusinessRules.Sonos.MaximumIdLength
            || !Enum.IsDefined(Kind) || Kind == SonosCommandKind.None
            || (Kind == SonosCommandKind.SetVolume
                ? Volume is null or < BusinessRules.Sonos.MinimumVolume or > BusinessRules.Sonos.MaximumVolume
                : Volume is not null))
        {
            throw new BusinessLogicException(SonosMessages.InvalidCommand);
        }
    }
}
