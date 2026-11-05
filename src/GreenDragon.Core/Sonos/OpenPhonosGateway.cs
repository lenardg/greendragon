using System.Globalization;
using System.Net.Http;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using OpenPhonos.Sonos;
using OpenPhonos.UPnP;
using SonosServices;

namespace GreenDragon.Core.Sonos;

/// <summary>Adapts request-driven OpenPhonos discovery and speaker reads to application contracts.</summary>
/// <param name="logger">Records individual speaker failures without discarding other rooms.</param>
public sealed class OpenPhonosGateway(ILogger<OpenPhonosGateway> logger) : ISonosGateway, IDisposable
{
    private const uint _instanceId = 0;
    private const string _masterChannel = "Master";
    private const string _mediaRendererDeviceType = "urn:schemas-upnp-org:device:MediaRenderer:1";
    private const string _renderingControlServiceId = "urn:upnp-org:serviceId:RenderingControl";
    private const string _playing = "PLAYING";
    private const string _paused = "PAUSED_PLAYBACK";
    private const string _stopped = "STOPPED";
    private const string _transitioning = "TRANSITIONING";
    private const string _noMedia = "NO_MEDIA_PRESENT";
    private static readonly XNamespace DcNamespace = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace UpnpNamespace = "urn:schemas-upnp-org:metadata-1-0/upnp/";
    private static readonly HashSet<string> ArtworkMediaTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    private readonly HttpClient _artworkClient = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
    {
        Timeout = BusinessRules.SonosArtworkTimeout
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Upstream has no cancellation overload. Await every call to preserve serialization.
        var result = await Household.FindAllHouseholdsAndPlayersAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(result.FailureReason))
        {
            throw new BusinessLogicException(SonosMessages.DiscoveryFailed);
        }
        var speakers = new List<SpeakerSnapshot>();
        foreach (var household in result.Households)
        {
            await household.BuildGroupsAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var playbackByCoordinator = new Dictionary<string, SpeakerPlaybackSnapshot>();
            foreach (var player in household.VisiblePlayerList.DistinctBy(player => player.UniqueName))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var coordinator = household.Groups.FirstOrDefault(group =>
                    group.VisiblePlayers.Any(member => member.UniqueName == player.UniqueName))?.Coordinator ?? player;
                if (!playbackByCoordinator.TryGetValue(coordinator.UniqueName, out var playback))
                {
                    playback = await ReadPlaybackAsync(coordinator, cancellationToken);
                    playbackByCoordinator.Add(coordinator.UniqueName, playback);
                }
                speakers.Add(await ReadRoomAsync(player, playback, cancellationToken));
            }
        }
        return speakers.DistinctBy(player => player.Id)
            .OrderBy(player => player.RoomName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _artworkClient.Dispose();
    }

    private async Task<SpeakerSnapshot> ReadRoomAsync(Player player, SpeakerPlaybackSnapshot playback,
        CancellationToken cancellationToken)
    {
        var snapshot = new SpeakerSnapshot(player.UniqueName, player.RoomName, player.ModelName) { Playback = playback };
        try
        {
            // Player keeps RenderingControl private; resolve the advertised service without changing upstream.
            var device = await Device.CreateAsync(player.DeviceProperties.BaseUri.AbsoluteUri);
            cancellationToken.ThrowIfCancellationRequested();
            var service = device.FindServiceInfo(_mediaRendererDeviceType, _renderingControlServiceId, throwIfMissing: false);
            if (service is null)
            {
                return snapshot;
            }
            var rendering = new RenderingControl1(service);
            var volume = await rendering.GetVolume(_instanceId, _masterChannel);
            cancellationToken.ThrowIfCancellationRequested();
            snapshot = snapshot with { Volume = volume.Error is null ? volume.CurrentVolume : null };
            var mute = await rendering.GetMute(_instanceId, _masterChannel);
            cancellationToken.ThrowIfCancellationRequested();
            return snapshot with { IsMuted = mute.Error is null ? mute.CurrentMute : null };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not read volume for speaker {SpeakerId}", player.UniqueName);
            return snapshot;
        }
    }

    private async Task<SpeakerPlaybackSnapshot> ReadPlaybackAsync(Player coordinator, CancellationToken cancellationToken)
    {
        var snapshot = new SpeakerPlaybackSnapshot();
        try
        {
            var transport = await coordinator.AVTransport.GetTransportInfo(_instanceId);
            cancellationToken.ThrowIfCancellationRequested();
            snapshot = snapshot with
            {
                State = transport.Error is null ? transport.CurrentTransportState switch
                {
                    _playing => SpeakerPlaybackState.Playing,
                    _paused => SpeakerPlaybackState.Paused,
                    _stopped => SpeakerPlaybackState.Stopped,
                    _transitioning => SpeakerPlaybackState.Transitioning,
                    _noMedia => SpeakerPlaybackState.NoMedia,
                    _ => SpeakerPlaybackState.Unknown
                } : SpeakerPlaybackState.Unknown
            };
            var position = await coordinator.AVTransport.GetPositionInfo(_instanceId);
            cancellationToken.ThrowIfCancellationRequested();
            if (position.Error is not null)
            {
                return snapshot;
            }
            snapshot = snapshot with { Position = ParseTime(position.RelTime), Duration = ParseTime(position.TrackDuration) };
            if (string.IsNullOrWhiteSpace(position.TrackMetaData))
            {
                return snapshot;
            }
            var metadata = XElement.Parse(position.TrackMetaData);
            snapshot = snapshot with
            {
                Title = metadata.Descendants(DcNamespace + "title").FirstOrDefault()?.Value,
                Artist = metadata.Descendants(DcNamespace + "creator").FirstOrDefault()?.Value,
                Album = metadata.Descendants(UpnpNamespace + "album").FirstOrDefault()?.Value
            };
            return snapshot with
            {
                CoverArtDataUrl = await ReadArtworkAsync(coordinator.AVTransport.BaseUri,
                    metadata.Descendants(UpnpNamespace + "albumArtURI").FirstOrDefault()?.Value, cancellationToken)
            };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not read playback for speaker {SpeakerId}", coordinator.UniqueName);
            return snapshot;
        }
    }

    private async Task<string?> ReadArtworkAsync(Uri speakerUri, string? art, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(art) || !Uri.TryCreate(speakerUri, art, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || (uri.Scheme != Uri.UriSchemeHttps &&
                (uri.Scheme != Uri.UriSchemeHttp || uri.Authority != speakerUri.Authority)))
        {
            return null;
        }
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(BusinessRules.SonosArtworkTimeout);
            using var response = await _artworkClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode || mediaType is null || !ArtworkMediaTypes.Contains(mediaType)
                || response.Content.Headers.ContentLength > BusinessRules.SonosArtworkMaximumBytes)
            {
                return null;
            }
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            // Read at most the configured limit plus one byte, including responses without Content-Length.
            var bytes = new byte[BusinessRules.SonosArtworkMaximumBytes + 1];
            var count = await stream.ReadAtLeastAsync(bytes, bytes.Length, throwOnEndOfStream: false, timeout.Token);
            return count <= BusinessRules.SonosArtworkMaximumBytes
                ? $"data:{mediaType};base64,{Convert.ToBase64String(bytes, 0, count)}" : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static TimeSpan? ParseTime(string? value)
    {
        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var time) && time >= TimeSpan.Zero ? time : null;
    }
}
