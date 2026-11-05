using GreenDragon.Core.Sonos;

namespace GreenDragon.Core.BusinessLogic;

/// <summary>Provides user initiated Sonos operations to the portal.</summary>
public interface ISonosBL
{
    /// <summary>Queues an explicit discovery request on the embedded worker.</summary>
    /// <param name="cancellationToken">Cancels the caller's pending request.</param>
    /// <returns>The visible rooms found by OpenPhonos.</returns>
    Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken);
}
