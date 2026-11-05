using GreenDragon.Core.Sonos;

namespace GreenDragon.Core.BusinessLogic;

/// <summary>Provides user initiated Sonos operations to the portal.</summary>
public interface ISonosBL
{
    /// <summary>Indicates whether the portal may request network operations.</summary>
    bool IsEnabled { get; }

    /// <summary>The configured browser refresh delay in seconds.</summary>
    int RefreshIntervalSeconds { get; }

    /// <summary>Queues an explicit discovery request on the embedded worker.</summary>
    /// <param name="cancellationToken">Cancels the caller's pending request.</param>
    /// <returns>The visible rooms found by OpenPhonos.</returns>
    /// <param name="rediscover">Whether the user explicitly requested rediscovery.</param>
    Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken, bool rediscover = false);

    /// <summary>Queues a fresh status read without repeating discovery.</summary>
    /// <param name="cancellationToken">Cancels the caller's wait.</param>
    /// <returns>Fresh room snapshots.</returns>
    Task<IReadOnlyList<SpeakerSnapshot>> RefreshAsync(CancellationToken cancellationToken);

    /// <summary>Validates and queues an explicit speaker command.</summary>
    /// <param name="command">The desired operation.</param>
    /// <param name="cancellationToken">Cancels the caller's wait.</param>
    /// <returns>Updated room snapshots.</returns>
    Task<IReadOnlyList<SpeakerSnapshot>> ExecuteAsync(SonosCommand command, CancellationToken cancellationToken);
}
