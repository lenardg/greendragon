using GreenDragon.Core.Configuration;
using GreenDragon.Core.Sonos;
using Microsoft.Extensions.Options;

namespace GreenDragon.Core.BusinessLogic;

/// <summary>Routes portal actions through the embedded Sonos worker.</summary>
/// <param name="worker">The application-owned command processor.</param>
/// <param name="settings">Controls whether the speaker overview can load.</param>
public sealed class SonosBL(SonosBackgroundService worker, IOptions<SonosSettings> settings) : ISonosBL
{
    /// <inheritdoc />
    public bool IsEnabled => settings.Value.Enabled;

    /// <inheritdoc />
    public int RefreshIntervalSeconds => settings.Value.RefreshIntervalSeconds;

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken, bool rediscover = false)
    {
        return worker.DiscoverAsync(cancellationToken, rediscover);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeakerSnapshot>> RefreshAsync(CancellationToken cancellationToken)
    {
        return worker.RefreshAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeakerSnapshot>> ExecuteAsync(SonosCommand command, CancellationToken cancellationToken)
    {
        command.Validate();
        return worker.ExecuteCommandAsync(command, cancellationToken);
    }
}
