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
    public async Task<SpeakerDetailsSnapshot> ReadPlayerAsync(string speakerId, CancellationToken cancellationToken)
    {
        var details = await worker.ReadPlayerAsync(speakerId, cancellationToken);
        var queue = details.Queue;
        var playback = details.Speaker.Playback;
        SpeakerQueueTrack? next = null;
        if (queue.IsAvailable && queue.IsActive && queue.CurrentTrackNumber > 0)
        {
            if (playback.Repeat == SonosRepeatMode.One)
            {
                next = queue.Tracks.FirstOrDefault(track => track.Number == queue.CurrentTrackNumber);
            }
            else if (playback.Shuffle == false)
            {
                next = queue.Tracks.FirstOrDefault(track => track.Number == queue.CurrentTrackNumber + 1);
                if (next is null && queue.CurrentTrackNumber == queue.TotalCount && playback.Repeat == SonosRepeatMode.All)
                {
                    next = queue.FirstTrack;
                }
            }
        }
        return details with { Queue = queue with { NextTrack = next } };
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeakerSnapshot>> ExecuteAsync(SonosCommand command, CancellationToken cancellationToken)
    {
        command.Validate();
        return worker.ExecuteCommandAsync(command, cancellationToken);
    }
}
