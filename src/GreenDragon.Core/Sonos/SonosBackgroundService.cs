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

    private async Task<IReadOnlyList<SpeakerSnapshot>> QueueAsync(
        Func<CancellationToken, Task<IReadOnlyList<SpeakerSnapshot>>> operation, CancellationToken cancellationToken)
    {
        if (!settings.Value.Enabled)
        {
            throw new BusinessLogicException(SonosMessages.Disabled);
        }
        var request = new SonosRequest(operation, cancellationToken);
        try
        {
            await _requests.Writer.WriteAsync(request, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            throw new BusinessLogicException(SonosMessages.Stopping);
        }
        return await request.Completion.Task.WaitAsync(cancellationToken);
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
                    var speakers = await request.Operation(linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    request.Completion.TrySetResult(speakers);
                }
                catch (OperationCanceledException) when (linked.IsCancellationRequested)
                {
                    request.Completion.TrySetCanceled(linked.Token);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Sonos request failed");
                    request.Completion.TrySetException(new BusinessLogicException(SonosMessages.DiscoveryFailed));
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
                request.Completion.TrySetCanceled(stoppingToken);
            }
        }
    }

    private sealed class SonosRequest(Func<CancellationToken, Task<IReadOnlyList<SpeakerSnapshot>>> operation, CancellationToken cancellationToken)
    {
        /// <summary>Allows queued work to be skipped after its caller disconnects.</summary>
        public CancellationToken CancellationToken { get; } = cancellationToken;
        /// <summary>The operation executed only by the single channel reader.</summary>
        public Func<CancellationToken, Task<IReadOnlyList<SpeakerSnapshot>>> Operation { get; } = operation;

        /// <summary>Returns the outcome to the caller without executing its continuation on the worker.</summary>
        public TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
