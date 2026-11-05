# Speaker controls - Spec

## Summary

Implement the requested interactive speaker cards in the current checkout. Keep the existing Razor Pages/VanillaJS approach and route discovery, refresh and commands through one bounded, sequential worker. See [goal](goal.md) and [work breakdown](wbs.md).

Status: implemented and verified in place. Changes remain unstaged and uncommitted.

## Current state

- [Architecture](../../../architecture.md), [UI](../../../frontend/pages.md) and [handlers](../../../backend/api.md) describe read-only snapshots loaded through an antiforgery-protected POST.
- SonosBackgroundService currently queues only DiscoverAsync. OpenPhonosGateway rediscovers households and reads coordinator playback plus individual-room volume/mute for each call.
- Index.cshtml contains the whole card; site.js fetches a full rendered page for initial loading/manual refresh. No control commands or timed refresh exist.
- The pinned OpenPhonos source exposes Previous, Play, Pause, Next, GetTransportSettings, SetPlayMode and GetCurrentTransportActions. RenderingControl is private on Player; the adapter already resolves its advertised service for reads.
- Current instructions forbid polling and playback controls. The latest user request authorizes these features; update the instructions to distinguish browser-triggered refresh from server background work. Never test live Sonos.
- The checkout was clean on codex/initial-project at the start of planning. The user's in-place-development instruction remains active; no branches, staging or commits.

## Acceptance criteria

- [x] AC1: Each card omits the speaker model, shows its track title above artwork, and limits artwork to approximately 10rem square while fitting narrow screens.
- [x] AC2: Enabled, visible pages refresh card status/position asynchronously about every five seconds after the previous request finishes. Disabled pages send no Sonos requests. Hidden pages pause refresh and resume when visible.
- [x] AC3: Previous, Play/Pause and Next use accessible SVG buttons; commands target the current group coordinator and the resulting group cards update.
- [x] AC4: Repeat and shuffle show reported states and can be changed with SVG buttons. Repeat cycles Off, All, One. Changing either preserves the other setting.
- [x] AC5: Volume shows a thicker adjustable 0-100 slider, percentage, and small minus/plus buttons. Minus/plus adjust the selected room by one percentage point and stop at the bounds.
- [x] AC6: Commands and reads share the existing bounded queue. Canceling a caller never allows the next operation to overlap an unfinished upstream call.
- [x] AC7: Antiforgery, server validation, safe metadata encoding, native form fallback, accessible errors/loading, focus and theme support remain intact.
- [x] AC8: Poll responses do not overwrite an active slider edit or newer command result. Failed commands are not automatically replayed; retry remains available.
- [x] AC9: Mocked tests and browser checks cover controls, refresh, security, disabled access and failure recovery. Formatting and a zero-warning build pass. No real Sonos access occurs during verification.
- [x] AC10: ASP.NET Core IMemoryCache shares player IDs and stable player information across requests, with a configured absolute lifetime of at least one hour. Repeated requests reuse cached metadata; playback status, position, volume and modes still refresh on their normal cadence. Future favorites, playlist and music-library retrieval uses the same minimum lifetime for browse results.

## Decisions

| ID | Topic | Proposed decision | Reason |
|---|---|---|---|
| D1 | Refresh cadence | Sonos:RefreshIntervalSeconds defaults to 5; validated positive bounded range. One completion-based browser timeout, paused while hidden. | Meets every-few-seconds updates without accumulating overlapping browser requests. |
| D2 | Discovery | SSDP only on initial discovery/manual rediscovery. Share the discovered device catalog through ASP.NET Core IMemoryCache with at least a one-hour metadata lifetime, and refresh topology/status for periodic requests. | Avoid repeating discovery or stable metadata retrieval on every tick; no persistent storage or background scans. |
| D3 | Command ordering | All refreshes and mutations enter the same existing queue. Re-read relevant state/topology inside a command before toggling modes or adjusting volume. | Preserves serialization and handles stale browser state or changed groups. |
| D4 | Playback scope | Playback, repeat and shuffle affect the current group; volume affects the chosen room. | Matches Sonos coordinator and rendering semantics. |
| D5 | Repeat/shuffle | Repeat Off -> All -> One -> Off; shuffle independently on/off. Map all six supported combinations; unknown states remain unavailable. | Exposes repeat-one and preserves independent settings. |
| D6 | Volume | Slider sends a whole percentage when committed; minus/plus change one point. Native form has an Apply volume action. | Avoids a request per pointer movement and works without JavaScript. |
| D7 | UI transport | Razor POST handlers with server-rendered card partials; fetch updates stable cards in place. Retain full-page native form results. | Reuses current rendering/encoding/localization without adding a frontend framework or a second JSON rendering path. |
| D8 | Errors/capabilities | Disable unavailable operations when known, expose current pressed states, retain cards on refresh failure and offer retry. Never automatically retry mutation requests. | Prevents duplicate Previous/Next/toggle commands and keeps context usable. |
| D9 | Workflow | Written plan review before implementation; edits remain unstaged and uncommitted. | Repository plan-change workflow combined with the user's in-place-development preference. |
| D10 | Shared metadata and browse cache | Register AddMemoryCache and inject IMemoryCache. Sonos:MetadataCacheDurationMinutes defaults to 60 and validates a minimum of 60. Cache player IDs/stable information and, when implemented, favorites, playlists and music-library results with absolute expiration. | Shares reusable results across requests for at least an hour while reducing latency and upstream calls. |

No additional product questions are required; the above defaults are reviewable choices.

## Design

### Core

- Extend ISonosBL/SonosBL with RefreshAsync and ExecuteAsync; validate speaker identifiers, command kinds and volume input in Core as well as Web binding.
- Introduce SonosCommand and SonosCommandKind for Previous, TogglePlayback, Next, CycleRepeat, ToggleShuffle, SetVolume, VolumeDown and VolumeUp.
- Extend SpeakerPlaybackSnapshot with nullable repeat/shuffle state and available transport actions. Keep explicit unavailable states rather than guessing defaults.
- Generalize SonosBackgroundService request envelopes to support discovery, refresh and commands through the same channel, cancellation and shutdown logic. Gateway operations finish before dequeuing subsequent work.
- Extend ISonosGateway with RefreshAsync and ExecuteAsync. Only OpenPhonosGateway owns upstream objects and network operations.
- OpenPhonosGateway uses the shared MemoryCache-backed device catalog for discovered households/player IDs. Refresh topology and read status without SSDP; resolve current coordinators before commands. Unknown/stale IDs produce safe errors, never arbitrary URL-based access.
- Add application-owned play-mode mapping for NORMAL, REPEAT_ALL, REPEAT_ONE, SHUFFLE_NOREPEAT, SHUFFLE and SHUFFLE_REPEAT_ONE. Map repeat/shuffle transitions without dropping the other flag.
- Reuse advertised rendering-service resolution for room volume writes; verify upstream ActionResult failures. No event subscriptions.
- Reuse unchanged artwork by source/track identity and discard entries for replaced catalog devices. Keep existing image type, size and timeout restrictions.
- Add refresh cadence to Configuration/SonosSettings and volume bounds/step to BusinessRules. Centralize protocol constants. Do not modify external/openphonos.

### Shared memory caching

- ASP.NET Core MemoryCache is registered with AddMemoryCache in SonosServiceCollectionExtensions and injected into the Core cache boundary. Use a focused SonosMetadataCache with an ISonosMetadataCache interface to own keys, retention and invalidation.
- Share discovered player IDs, stable player information and the gateway's catalog between requests in this host process. Expose immutable application metadata to consumers; keep upstream Player/Household objects private to the gateway and access them only through the sequential worker.
- Add SonosSettings.MetadataCacheDurationMinutes, default 60, with server-side options validation requiring at least 60 minutes. Put the minimum/default duration in BusinessRules. Use absolute expiration from population, not sliding expiration alone.
- Populate on a requested cache miss through the existing queued gateway operation. Reuse populated entries across subsequent requests; concurrent callers must not launch duplicate upstream retrievals outside the worker. Cache expiration never starts background discovery or other network work.
- Keep rapidly changing transport state, position, volume, mute, repeat, shuffle and group membership fresh on the existing refresh/command path. A one-hour metadata lifetime must not make live card values stale for an hour.
- Explicit rediscovery replaces the catalog and invalidates affected metadata; confirmed device removal or a relevant successful mutation can invalidate its entries. Expired/missing catalog entries follow the existing actionable rediscovery flow, without silent recurring SSDP scans.
- When favorites, playlist or music-library retrieval is added, cache successful browse results using the same at-least-one-hour retention. Include household/source, collection or object ID, paging, sort/filter and other result-shaping parameters in keys. Invalidate affected collections after future mutations or an explicit user refresh; do not cache retrieval failures as successful empty results.
- Favorites, playlist and music-library retrieval/UI remain future work. This plan records their caching policy without adding those domain features to the speaker-controls increment.
- The cache is process-local, nonpersistent and refillable after expiration, eviction or restart; it introduces neither a database nor a server scheduler.

### Web

- Extract a reusable card partial from Index.cshtml. Keep the room heading; put track title/artist/album before artwork; remove model text. Limit artwork and its placeholder with the same responsive dimensions.
- Add speaker-scoped transport and mode form buttons with inline SVG paths, localized accessible names, visible focus, aria-pressed where applicable and unavailable explanations. Icons use currentColor and no external SVG dependency.
- Add a labeled range input with percentage, minus/plus and native Apply volume submission. Keep playback position separate from volume.
- Add thin OnPostRefreshAsync and OnPostCommandAsync handlers. A bound SpeakerCommandAIM under Models validates IDs, enum values and volume; Core remains responsible for decisions. Preserve antiforgery on every POST.
- Fetch receives rendered card results and errors; normal form POST receives the page. Stable speaker keys identify cards without interpolating device metadata into executable markup/selectors.
- site.js coordinates initial discovery, periodic refresh and commands with async fetch. Send antiforgery tokens, stop duplicate submissions and pause scheduled reads during commands/volume edits. Ignore superseded results, retain focused controls and do not replace a range input during a drag.
- On command success update all affected group cards promptly. On refresh errors retain existing cards with stale/error feedback. Pause refresh on visibility/page lifecycle changes; aborting a fetch only cancels its caller wait.
- Update CSS variables/styles and shared English JSON strings. Preserve System/Light/Dark and reduced-motion behavior.

## Data contracts

| Contract | Fields / result |
|---|---|
| SonosCommand | SpeakerId, Kind, optional Volume for SetVolume |
| SpeakerPlaybackSnapshot additions | RepeatMode: Off/All/One/unknown, Shuffle: true/false/unknown, supported transport actions |
| Shared metadata cache | Household/source-scoped player IDs and stable player information; absolute expiry from Sonos:MetadataCacheDurationMinutes, default/minimum 60 |
| Future browse cache | Favorites/playlist/library result keyed by household/source and full query shape; same minimum one-hour retention |
| ISonosBL / ISonosGateway additions | RefreshAsync(CancellationToken), ExecuteAsync(SonosCommand, CancellationToken), returning refreshed application snapshots |
| POST /?handler=Refresh | Antiforgery token; snapshot card fragment for fetch, full page for native requests |
| POST /?handler=Command | Antiforgery token plus validated command fields; refreshed cards or retryable validation/operation error |

No GET, health route, startup or server idle time invokes Sonos. Periodic work exists only while an enabled browser page requests it.

## Testing

- Worker tests mock ISonosGateway and cover mixed discovery/read/write ordering, bounded waiting, disabled access, cancellation, failure recovery and shutdown.
- Core tests exercise application-owned command validation, volume bounds and mode transitions; they do not invoke OpenPhonos or its tests.
- Focused cache tests use a real in-memory cache with a controllable clock and a mocked ISonosGateway. Verify reuse across requests, the one-hour minimum configuration, expiration/invalidation and safe queued repopulation without waiting an hour or contacting devices. Verify live-state reads remain fresh independently of metadata retention; future browse features must test their scoped query keys and invalidation when implemented.
- Web integration tests mock ISonosGateway and cover each command, validation before device access, antiforgery, disabled configuration, refreshed cards, group behavior, localized accessible labels and malicious metadata encoding.
- A temporary host replaces ISonosGateway before startup for browser checks: polling without overlapping requests, control results, volume editing/focus, retry, hidden-tab pause, native form handling, and phone/tablet/desktop light/dark layouts.
- Run solution format/verify/build and the applicable test suites. If the existing dotnet test handshake issue recurs, run the built xUnit executables and report that limitation.

## Documentation impact

- Root/Core AGENTS.md: authorize browser-driven status refresh and implemented commands while preserving one queue, disabled defaults and offline verification.
- Architecture/integrations: shared IMemoryCache catalog, minimum one-hour metadata retention, lazy population/invalidation, future browse-result caching, sequential reads/writes, coordinator versus room scope and artwork reuse.
- Backend/frontend: refresh/control POSTs, antiforgery, forms, polling lifecycle, state/error handling and accessibility.
- Getting started/environment: refresh and metadata cache-duration configuration, process-local cache behavior and no live-device verification.
- Manual testing/README: new controls, repeat cycle, volume behavior and mocked verification. Keep the existing documentation index current if its descriptions need changing.

## Risks and notes

- A five-second cadence is a target; slow upstream reads extend the interval. Do not start a second operation to catch up.
- Cache expiry/eviction, a server restart or a disappeared speaker can invalidate cached IDs. Display an actionable rediscovery message; periodic refresh must not silently start repeated discovery scans. A configured one-hour lifetime is not durable storage and does not prevent explicit invalidation or memory-pressure eviction.
- Some sources do not support skipping, pausing or play modes. Report unsupported operations safely and keep remaining controls usable.
- Browser timers must never become server schedulers. Existing upstream cancellation limitations still apply.
- The user approved implementation including memory caching. All application changes remain unstaged and uncommitted.

## Verification record

- 22 Core tests and 27 Web integration tests pass through the direct xUnit executables. All device access is replaced by ISonosGateway substitutes.
- A stateful mock-host browser pass verified the card layout and interactive controls, periodic updates, volume editing/focus and retry; see the [manual execution log](../../../manual-testing.md#execution-log).
- The in-app browser reports virtual tabs as visible, so true hidden-tab pause/resume was verified by code inspection rather than a manual visibility transition. Native form handling is verified by integration tests.
- Build uses a separate bin/PlanCheck output directory to avoid locking or stopping the user's running web app. Final format/build/check results are recorded in the work breakdown.

- Final format/verify checks pass and the solution build has zero warnings/errors. The solution-level test command still has its pre-existing Microsoft.Testing.Platform handshake failure; direct xUnit execution passes all 49 tests.
