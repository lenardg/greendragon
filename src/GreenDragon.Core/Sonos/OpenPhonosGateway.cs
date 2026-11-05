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
/// <param name="cache">Shares stable metadata between requests.</param>
public sealed class OpenPhonosGateway(ILogger<OpenPhonosGateway> logger, ISonosMetadataCache cache) : ISonosGateway, IDisposable
{
    private const string _queueId = "Q:0";
    private const string _browseChildren = "BrowseDirectChildren";
    private const string _allMetadata = "*";
    private const string _queueUriPrefix = "x-rincon-queue:";
    private static readonly XNamespace DidlNamespace = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
    private const string _catalogArea = "Sonos.Catalog";
    private const string _catalogKey = "Households";
    private const string _renderingArea = "Sonos.Rendering";
    private const string _artworkArea = "Sonos.Artwork";
    private const string _refreshReason = "GreenDragon request";
    private const string _playAction = "Play";
    private const string _pauseAction = "Pause";
    private const string _previousAction = "Previous";
    private const string _nextAction = "Next";
    private const string _normalSpeed = "1";
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
    public async Task<IReadOnlyList<SpeakerSnapshot>> DiscoverAsync(CancellationToken cancellationToken, bool rediscover = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!rediscover && cache.TryGet<Catalog>(_catalogArea, _catalogKey, out var existing))
        {
            return await ReadSnapshotsAsync(existing, cancellationToken);
        }
        // Upstream has no cancellation overload. Await every call to preserve serialization.
        var result = await Household.FindAllHouseholdsAndPlayersAsync();
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(result.FailureReason))
        {
            throw new BusinessLogicException(SonosMessages.DiscoveryFailed);
        }
        if (cache.TryGet<Catalog>(_catalogArea, _catalogKey, out var previous))
        {
            foreach (var player in previous.Households.SelectMany(household => household.AllPlayerList))
            {
                cache.Remove<RenderingControl1>(_renderingArea, RenderingKey(player));
                cache.Remove<Artwork>(_artworkArea, player.UniqueName);
            }
        }
        var catalog = new Catalog(result.Households);
        cache.Set(_catalogArea, _catalogKey, catalog, SonosCachePolicy.PlayerCatalog);
        return await ReadSnapshotsAsync(catalog, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeakerSnapshot>> RefreshAsync(CancellationToken cancellationToken)
    {
        return ReadSnapshotsAsync(GetCatalog(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerSnapshot>> ExecuteAsync(SonosCommand command, CancellationToken cancellationToken)
    {
        command.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var catalog = GetCatalog();
        await UpdateTopologyAsync(catalog, cancellationToken);
        var household = catalog.Households.FirstOrDefault(item => item.VisiblePlayerList.Any(player => player.UniqueName == command.SpeakerId));
        var player = household?.VisiblePlayerList.FirstOrDefault(item => item.UniqueName == command.SpeakerId)
            ?? throw new BusinessLogicException(SonosMessages.Rediscover);
        var coordinator = Coordinator(household!, player);
        Service.ActionResult response;
        switch (command.Kind)
        {
            case SonosCommandKind.Previous:
                response = await coordinator.AVTransport.Previous(_instanceId);
                break;
            case SonosCommandKind.Next:
                response = await coordinator.AVTransport.Next(_instanceId);
                break;
            case SonosCommandKind.TogglePlayback:
                var transport = await coordinator.AVTransport.GetTransportInfo(_instanceId);
                cancellationToken.ThrowIfCancellationRequested();
                transport.ThrowIfFailed();
                response = transport.CurrentTransportState == _playing
                    ? await coordinator.AVTransport.Pause(_instanceId)
                    : await coordinator.AVTransport.Play(_instanceId, _normalSpeed);
                break;
            case SonosCommandKind.CycleRepeat:
            case SonosCommandKind.ToggleShuffle:
                var settings = await coordinator.AVTransport.GetTransportSettings(_instanceId);
                cancellationToken.ThrowIfCancellationRequested();
                settings.ThrowIfFailed();
                var mode = SonosPlayMode.Parse(settings.PlayMode) ?? throw new BusinessLogicException(SonosMessages.Unsupported);
                response = await coordinator.AVTransport.SetPlayMode(_instanceId, mode.Change(command.Kind).ToProtocol());
                break;
            default:
                var rendering = await GetRenderingAsync(player, cancellationToken)
                    ?? throw new BusinessLogicException(SonosMessages.Unsupported);
                var volume = command.Volume;
                if (command.Kind != SonosCommandKind.SetVolume)
                {
                    var current = await rendering.GetVolume(_instanceId, _masterChannel);
                    cancellationToken.ThrowIfCancellationRequested();
                    current.ThrowIfFailed();
                    var delta = command.Kind == SonosCommandKind.VolumeUp
                        ? BusinessRules.Sonos.VolumeStep : -BusinessRules.Sonos.VolumeStep;
                    volume = Math.Clamp(current.CurrentVolume + delta, BusinessRules.Sonos.MinimumVolume, BusinessRules.Sonos.MaximumVolume);
                }
                cancellationToken.ThrowIfCancellationRequested();
                response = await rendering.SetVolume(_instanceId, _masterChannel, (ushort)volume!.Value);
                break;
        }
        // Every upstream action must finish before the next queued operation can run.
        cancellationToken.ThrowIfCancellationRequested();
        response.ThrowIfFailed();
        return await ReadSnapshotsAsync(catalog, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SpeakerDetailsSnapshot> ReadPlayerAsync(string speakerId, CancellationToken cancellationToken)
    {
        var catalog = GetCatalog();
        await UpdateTopologyAsync(catalog, cancellationToken);
        var household = catalog.Households.FirstOrDefault(item => item.VisiblePlayerList.Any(player => player.UniqueName == speakerId));
        var player = household?.VisiblePlayerList.FirstOrDefault(item => item.UniqueName == speakerId)
            ?? throw new BusinessLogicException(SonosMessages.Rediscover);
        var coordinator = Coordinator(household!, player);
        var playback = await ReadPlaybackAsync(coordinator, cancellationToken);
        var speaker = await ReadRoomAsync(player, playback, cancellationToken);
        return new SpeakerDetailsSnapshot(speaker, await ReadQueueAsync(coordinator, cancellationToken));
    }

    private async Task<SpeakerQueueSnapshot> ReadQueueAsync(Player coordinator, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var media = await coordinator.AVTransport.GetMediaInfo(_instanceId);
            cancellationToken.ThrowIfCancellationRequested();
            media.ThrowIfFailed();
            if (media.CurrentURI?.StartsWith(_queueUriPrefix, StringComparison.OrdinalIgnoreCase) != true)
            {
                return new SpeakerQueueSnapshot { IsAvailable = true };
            }
            var position = await coordinator.AVTransport.GetPositionInfo(_instanceId);
            cancellationToken.ThrowIfCancellationRequested();
            position.ThrowIfFailed();
            var start = position.Track > 0 ? position.Track - 1 : 0;
            var result = await coordinator.ContentDirectory.Browse(_queueId, _browseChildren, _allMetadata,
                start, BusinessRules.Sonos.QueuePreviewLength, string.Empty);
            cancellationToken.ThrowIfCancellationRequested();
            result.ThrowIfFailed();
            var tracks = ParseQueueTracks(result.Result, start);
            var first = tracks.FirstOrDefault(track => track.Number == 1);
            // The first entry is needed to describe repeat-all at the end of a queue.
            if (position.Track == result.TotalMatches && position.Track > 1)
            {
                var beginning = await coordinator.ContentDirectory.Browse(_queueId, _browseChildren, _allMetadata, 0, 1, string.Empty);
                cancellationToken.ThrowIfCancellationRequested();
                beginning.ThrowIfFailed();
                first = ParseQueueTracks(beginning.Result, 0).FirstOrDefault();
            }
            return new SpeakerQueueSnapshot
            {
                IsAvailable = true,
                IsActive = true,
                TotalCount = result.TotalMatches,
                CurrentTrackNumber = position.Track,
                Tracks = tracks,
                FirstTrack = first
            };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not read queue for speaker {SpeakerId}", coordinator.UniqueName);
            return new SpeakerQueueSnapshot();
        }
    }

    private static IReadOnlyList<SpeakerQueueTrack> ParseQueueTracks(string metadata, uint start)
    {
        return XElement.Parse(metadata).Elements(DidlNamespace + "item")
            .Take(BusinessRules.Sonos.QueuePreviewLength)
            .Select((item, index) => new SpeakerQueueTrack(start + (uint)index + 1,
                item.Element(DcNamespace + "title")?.Value,
                item.Element(DcNamespace + "creator")?.Value,
                item.Element(UpnpNamespace + "album")?.Value)).ToArray();
    }

    private Catalog GetCatalog()
    {
        return cache.TryGet<Catalog>(_catalogArea, _catalogKey, out var catalog)
            ? catalog : throw new BusinessLogicException(SonosMessages.Rediscover);
    }

    private static Player Coordinator(Household household, Player player)
    {
        return household.Groups.FirstOrDefault(group => group.VisiblePlayers.Any(member => member.UniqueName == player.UniqueName))
            ?.Coordinator ?? player;
    }

    private static async Task UpdateTopologyAsync(Catalog catalog, CancellationToken cancellationToken)
    {
        foreach (var household in catalog.Households)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = await household.AssociatedZP.GetZoneGroupStateAsync();
            cancellationToken.ThrowIfCancellationRequested();
            await household.UpdateFromZoneStateAsync(state, _refreshReason);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task<IReadOnlyList<SpeakerSnapshot>> ReadSnapshotsAsync(Catalog catalog, CancellationToken cancellationToken)
    {
        await UpdateTopologyAsync(catalog, cancellationToken);
        var speakers = new List<SpeakerSnapshot>();
        foreach (var household in catalog.Households)
        {
            var playbackByCoordinator = new Dictionary<string, SpeakerPlaybackSnapshot>();
            foreach (var player in household.VisiblePlayerList.DistinctBy(player => player.UniqueName))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var coordinator = Coordinator(household, player);
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

    private static string RenderingKey(Player player)
    {
        return $"{player.UniqueName}|{player.DeviceProperties?.BaseUri}";
    }

    private async Task<RenderingControl1?> GetRenderingAsync(Player player, CancellationToken cancellationToken)
    {
        var key = RenderingKey(player);
        if (cache.TryGet<RenderingControl1>(_renderingArea, key, out var rendering))
        {
            return rendering;
        }
        cancellationToken.ThrowIfCancellationRequested();
        var device = await Device.CreateAsync(player.DeviceProperties.BaseUri.AbsoluteUri);
        cancellationToken.ThrowIfCancellationRequested();
        var service = device.FindServiceInfo(_mediaRendererDeviceType, _renderingControlServiceId, throwIfMissing: false);
        if (service is null)
        {
            return null;
        }
        rendering = new RenderingControl1(service);
        cache.Set(_renderingArea, key, rendering);
        return rendering;
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
            var rendering = await GetRenderingAsync(player, cancellationToken);
            if (rendering is null)
            {
                return snapshot;
            }
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
            var actions = await coordinator.AVTransport.GetCurrentTransportActions(_instanceId);
            cancellationToken.ThrowIfCancellationRequested();
            var available = actions.Error is null
                ? (actions.Actions ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                : [];
            snapshot = snapshot with
            {
                CanPrevious = available.Contains(_previousAction, StringComparer.OrdinalIgnoreCase),
                CanNext = available.Contains(_nextAction, StringComparer.OrdinalIgnoreCase),
                CanTogglePlayback = available.Contains(snapshot.State == SpeakerPlaybackState.Playing ? _pauseAction : _playAction,
                    StringComparer.OrdinalIgnoreCase)
            };
            var settings = await coordinator.AVTransport.GetTransportSettings(_instanceId);
            cancellationToken.ThrowIfCancellationRequested();
            var mode = settings.Error is null ? SonosPlayMode.Parse(settings.PlayMode) : null;
            snapshot = snapshot with { Repeat = mode?.Repeat, Shuffle = mode?.Shuffle };
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
                CoverArtDataUrl = await ReadArtworkAsync(coordinator.UniqueName, coordinator.AVTransport.BaseUri,
                    metadata.Descendants(UpnpNamespace + "albumArtURI").FirstOrDefault()?.Value, cancellationToken)
            };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not read playback for speaker {SpeakerId}", coordinator.UniqueName);
            return snapshot;
        }
    }

    private async Task<string?> ReadArtworkAsync(string playerId, Uri speakerUri, string? art, CancellationToken cancellationToken)
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
            if (cache.TryGet<Artwork>(_artworkArea, playerId, out var cached) && cached.Uri == uri)
            {
                return cached.Data;
            }
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
            if (count == 0 || count > BusinessRules.SonosArtworkMaximumBytes)
            {
                return null;
            }
            var data = $"data:{mediaType};base64,{Convert.ToBase64String(bytes, 0, count)}";
            cache.Set(_artworkArea, playerId, new Artwork(uri, data));
            return data;
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
    private sealed record Catalog(IReadOnlyList<Household> Households);
    private sealed record Artwork(Uri Uri, string Data);
}
