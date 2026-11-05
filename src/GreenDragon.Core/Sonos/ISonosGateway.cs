namespace GreenDragon.Core.Sonos;

/// <summary>Isolates all OpenPhonos network access so tests replace it completely.</summary>
public interface ISonosGateway
{
    /// <summary>Discovers rooms only when explicitly requested.</summary>
    /// <param name="cancellationToken">Cancels before or after the upstream operation.</param>
    /// <returns>The discovered rooms.</returns>
    Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken);
}
