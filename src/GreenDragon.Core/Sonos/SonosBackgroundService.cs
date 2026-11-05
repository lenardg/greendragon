using System.Threading.Channels;
using GreenDragon.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPhonos.UPnP;

namespace GreenDragon.Core.Sonos;

/// <summary>Executes user requests sequentially and idles without polling or startup discovery.</summary>
/// <param name="gateway">The replaceable OpenPhonos boundary.</param>
/// <param name="settings">The network access setting.</param>
/// <param name="logger">Records failed requests.</param>
/// <param name="loggerFactory">Routes upstream logging to the application's providers.</param>
public sealed class SonosBackgroundService(ISonosGateway gateway, IOptions<SonosSettings> settings,
    ILogger<SonosBackgroundService> logger, ILoggerFactory loggerFactory) : BackgroundService
{
    private readonly Channel<SonosRequest> _requests = Channel.CreateBounded<SonosRequest>(
        new BoundedChannelOptions(BusinessRules.SonosQueueCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <summary>Queues a discovery request and waits for its result.</summary>
    /// <param name="cancellationToken">Cancels the pending request or wait.</param>
    /// <returns>The rooms reported by the gateway.</returns>
    /// <param name="rediscover">Whether to replace cached discovery.</param>
    /// <exception cref="BusinessLogicException">Network access is disabled or the host is stopping.</exception>
    public Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken, bool rediscover = false)
    {
        return QueueAsync(token => gateway.DiscoverAsync(token, rediscover), cancellationToken);
    }

    /// <summary>Queues a status read on the same bounded channel as commands.</summary>
    /// <param name="cancellationToken">Cancels the pending request or wait.</param>
    /// <returns>Fresh room snapshots.</returns>
    public Task<IReadOnlyList<SpeakerSnapshot>> RefreshAsync(CancellationToken cancellationToken)
    {
        return QueueAsync(gateway.RefreshAsync, cancellationToken);
    }

    /// <summary>Queues a validated mutation without bypassing serialization.</summary>
    /// <param name="command">The requested operation.</param>
    /// <param name="cancellationToken">Cancels the pending request or wait.</param>
    /// <returns>Updated room snapshots.</returns>
    public Task<IReadOnlyList<SpeakerSnapshot>> ExecuteCommandAsync(SonosCommand command, CancellationToken cancellationToken)
    {
        command.Validate();
        return QueueAsync(token => gateway.ExecuteAsync(command, token), cancellationToken);
    }

    /// <summary>Queues a selected-player read alongside discovery and commands.</summary>
    /// <param name="speakerId">The known player identifier.</param>
    /// <param name="cancellationToken">Cancels waiting without overlapping upstream operations.</param>
    /// <returns>The room snapshot and queue preview.</returns>
    public Task<SpeakerDetailsSnapshot> ReadPlayerAsync(string speakerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(speakerId) || speakerId.Length > BusinessRules.Sonos.MaximumIdLength)
        {
            throw new BusinessLogicException(SonosMessages.InvalidCommand);
        }
        return QueueAsync(token => gateway.ReadPlayerAsync(speakerId, token), cancellationToken);
    }

    private async Task<T> QueueAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!settings.Value.Enabled)
        {
            throw new BusinessLogicException(SonosMessages.Disabled);
        }
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new SonosRequest(async token =>
        {
            var result = await operation(token);
            token.ThrowIfCancellationRequested();
            completion.TrySetResult(result);
        }, cancellationToken, exception => completion.TrySetException(exception), token => completion.TrySetCanceled(token));
        try
        {
            await _requests.Writer.WriteAsync(request, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            throw new BusinessLogicException(SonosMessages.Stopping);
        }
        return await completion.Task.WaitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _requests.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        NetLogger.LoggerFactory = loggerFactory;
        UPnPConfig.ListenerName = SonosPlatform.ApplicationName;
        UPnPConfig.UserAgent = SonosPlatform.UserAgent;
        Platform.Instance = new SonosPlatform(logger);
        try
        {
            await foreach (var request in _requests.Reader.ReadAllAsync(stoppingToken))
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, request.CancellationToken);
                try
                {
                    linked.Token.ThrowIfCancellationRequested();
                    await request.Operation(linked.Token);
                }
                catch (OperationCanceledException) when (linked.IsCancellationRequested)
                {
                    request.Cancel(linked.Token);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Sonos request failed");
                    request.Fail(new BusinessLogicException(SonosMessages.DiscoveryFailed));
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host cancellation is normal; complete pending callers below.
        }
        finally
        {
            _requests.Writer.TryComplete();
            while (_requests.Reader.TryRead(out var request))
            {
                request.Cancel(stoppingToken);
            }
        }
    }

    private sealed record SonosRequest(
        Func<CancellationToken, Task> Operation,
        CancellationToken CancellationToken,
        Action<Exception> Fail,
        Action<CancellationToken> Cancel);
}
