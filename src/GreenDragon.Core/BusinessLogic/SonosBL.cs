using GreenDragon.Core.Sonos;

namespace GreenDragon.Core.BusinessLogic;

/// <summary>Routes portal actions through the embedded Sonos worker.</summary>
/// <param name="worker">The application-owned command processor.</param>
public sealed class SonosBL(SonosBackgroundService worker) : ISonosBL
{
    /// <inheritdoc />
    public Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken)
    {
        return worker.DiscoverAsync(cancellationToken);
    }
}
