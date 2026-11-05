using OpenPhonos.Sonos;

namespace GreenDragon.Core.Sonos;

/// <summary>Adapts the pinned OpenPhonos discovery API to application contracts.</summary>
public sealed class OpenPhonosGateway : ISonosGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Upstream has no cancellation overload. Await completion to preserve serialization.
        var result = await Household.FindAllHouseholdsAndPlayersAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(result.FailureReason))
        {
            throw new BusinessLogicException(SonosMessages.DiscoveryFailed);
        }
        return result.Households.SelectMany(household => household.VisiblePlayerList)
            .Select(player => new SpeakerSnapshot(player.UniqueName, player.RoomName, player.ModelName))
            .DistinctBy(player => player.Id)
            .OrderBy(player => player.RoomName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
