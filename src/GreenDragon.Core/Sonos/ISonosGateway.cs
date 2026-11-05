namespace GreenDragon.Core.Sonos;

/// <summary>Isolates all OpenPhonos network access so tests replace it completely.</summary>
public interface ISonosGateway
{
    /// <summary>Discovers rooms only when explicitly requested.</summary>
    /// <param name="cancellationToken">Cancels before or after the upstream operation.</param>
    /// <returns>The discovered rooms.</returns>
    /// <param name="rediscover">Replaces cached discovery only when explicitly requested.</param>
    Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken, bool rediscover = false);

    /// <summary>Reads fresh status from the shared catalog without SSDP discovery.</summary>
    /// <param name="cancellationToken">Cancels waiting before or after upstream calls.</param>
    /// <returns>Current room snapshots.</returns>
    Task<IReadOnlyList<SpeakerSnapshot>> RefreshAsync(CancellationToken cancellationToken);

    /// <summary>Executes a command and reads the resulting state inside the sequential worker.</summary>
    /// <param name="command">The validated command addressed to a known player.</param>
    /// <param name="cancellationToken">Cancels waiting before or after upstream calls.</param>
    /// <returns>Updated room snapshots.</returns>
    Task<IReadOnlyList<SpeakerSnapshot>> ExecuteAsync(SonosCommand command, CancellationToken cancellationToken);
}
