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
    private readonly Channel<DiscoveryRequest> _requests = Channel.CreateBounded<DiscoveryRequest>(
        new BoundedChannelOptions(BusinessRules.SonosQueueCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <summary>Queues a discovery request and waits for its result.</summary>
    /// <param name="cancellationToken">Cancels the pending request or wait.</param>
    /// <returns>The rooms reported by the gateway.</returns>
    /// <exception cref="BusinessLogicException">Network access is disabled or the host is stopping.</exception>
    public async Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken)
    {
        if (!settings.Value.Enabled)
        {
            throw new BusinessLogicException(SonosMessages.Disabled);
        }
        var request = new DiscoveryRequest(cancellationToken);
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
                    var speakers = await gateway.DiscoverAsync(linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    request.Completion.TrySetResult(speakers);
                }
                catch (OperationCanceledException) when (linked.IsCancellationRequested)
                {
                    request.Completion.TrySetCanceled(linked.Token);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Sonos discovery request failed");
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

    private sealed class DiscoveryRequest(CancellationToken cancellationToken)
    {
        /// <summary>Allows queued work to be skipped after its caller disconnects.</summary>
        public CancellationToken CancellationToken { get; } = cancellationToken;

        /// <summary>Returns the outcome to the caller without executing its continuation on the worker.</summary>
        public TaskCompletionSource<IReadOnlyList<SpeakerSnapshot>> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
