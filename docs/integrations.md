# Integrations

## SUMMARY

OpenPhonos provides local Sonos discovery, status reads and playback control. All automated tests replace its application boundary.

## OpenPhonos

Source: [amp64/openphonos](https://github.com/amp64/openphonos), MIT license retained at external/openphonos/LICENSE. The Git submodule pins the exact commit. There is no OpenPhonos NuGet package. Use git submodule status to inspect the revision.

The two source-build projects under src compile the unmodified Sonos and UPnP sources for .NET 10. Their original project files are not used: System.Text.Json comes from the shared runtime, and Logging.Abstractions uses the solution's current centrally managed version. This avoids inheriting obsolete dependency versions or prerelease package identities.

Upstream sources are exempt from application formatting, XML documentation and nullable requirements. Only the Sonos wrapper suppresses the upstream unused-local warnings CS0168 and CS0219. No application warning is suppressed. Do not format or rewrite the submodule as part of application changes.

ISonosGateway is the single replaceable network boundary. OpenPhonosGateway discovers visible rooms and reads group-coordinator playback plus per-room volume/mute into SpeakerSnapshot records. All reads, including optional artwork downloads, remain inside the serialized request. Failed optional reads display unavailable values. Artwork is embedded as a size-limited raster data URL so HTTP speaker artwork works on the HTTPS portal; redirects are disabled, and plain HTTP artwork must originate from the speaker. SonosPlatform supplies server platform behavior, and library logs use the configured application ILoggerFactory. Discovery exceptions are logged and translated to safe portal errors.

Discovery, refresh and control commands all use Core business logic and the same sequential worker. The gateway reuses its shared cached catalog for status reads and only performs SSDP on a missing initial catalog or explicit rediscovery. Playback and mode changes resolve the current group coordinator; volume applies to the selected room. The application creates no event subscriptions or server scheduled work. SonosPlayMode maps all six repeat/shuffle combinations and preserves the unaffected setting.

## Maintenance and verification

Update the submodule deliberately to a reviewed commit, then rebuild the wrappers and run all mocked tests. Do not run upstream test projects, SSDP scans, playback commands or any integration test against live Sonos devices. A successful mocked test verifies our wiring, not the underlying Sonos protocol.

## Shared metadata caching

ISonosMetadataCache uses ASP.NET Core IMemoryCache for stable player/catalog metadata, rendering service descriptions and successful unchanged artwork. Player IDs and the discovered catalog use SonosCachePolicy.PlayerCatalog: 24-hour sliding expiration, renewed by each successful read. Other entries use MetadataAbsolute: Sonos:MetadataCacheDurationMinutes defaults to 60 and validates a minimum of 60 minutes, with absolute expiration that reads do not extend. Only the sequential gateway populates upstream data. Expiration, eviction and restart do not launch network work; the next user request can require explicit rediscovery. Explicit rediscovery replaces the catalog and invalidates affected service/artwork entries.

Live status, position, volume, mute, repeat, shuffle and group membership use fresh reads regardless of metadata retention. The cache is process-local and nonpersistent.

Future favorites, playlist and music-library retrieval should use the same minimum one-hour retention, keyed by household/source plus the complete browse query (object/collection ID, paging, filters and sorting). Invalidate affected successful results after explicit refresh or mutation; do not cache failures as empty results. These retrieval/UI features are not implemented yet.
