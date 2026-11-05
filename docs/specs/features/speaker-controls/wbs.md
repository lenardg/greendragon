# Speaker controls - Work breakdown

## Planning

- ✅ Write goal.md from the user's requests and screenshot.
- ✅ Inspect repository instructions, documentation, existing queue, gateway, cards and tests.
- ✅ Write spec.md with acceptance criteria, decisions, contracts and verification.
- ✅ Incorporate shared ASP.NET Core MemoryCache with at least one-hour metadata retention and future browse-result caching.
- ✅ User approved the plan including memory caching; implement in place without commits.
- ❌ Commit plan or implementation: the user's continuing in-place-development instruction requires unstaged, uncommitted edits.

## Design

- ✅ Finalize Core command, repeat-mode and capability contracts.
- ✅ Define refresh behavior, native forms and focus-preserving card updates.
- ✅ Define scoped metadata/browse cache keys, minimum one-hour expiration, invalidation and separation from live playback state.

## Implementation

### Core

- ✅ Extend ISonosBL and ISonosGateway with snapshot refresh and control operations.
- ✅ Generalize SonosBackgroundService requests while preserving one bounded queue, shutdown behavior and in-flight serialization.
- ✅ Register AddMemoryCache and implement the shared IMemoryCache-backed discovered-device catalog and metadata cache boundary; refresh topology/status without repeating SSDP discovery every cycle.
- ✅ Add validated Sonos:MetadataCacheDurationMinutes (default/minimum 60), absolute expiration and explicit invalidation; document the same policy for future favorites, playlist and music-library queries.
- ✅ Read and map play modes and transport capabilities; implement coordinator playback/mode commands and per-room volume changes.
- ✅ Reuse unchanged artwork; enforce existing artwork size/time limits and discard stale catalog/cache entries.
- ✅ Put refresh/volume defaults and protocol identifiers in appropriate settings/constants containers.

### Web

- ✅ Extract reusable cards, remove model text, move the track title above artwork and reduce artwork size.
- ✅ Add validated Razor refresh/command POST handlers with antiforgery and native-form fallback.
- ✅ Add SVG playback/mode buttons with localized accessible names, pressed states and unavailable states.
- ✅ Add volume slider, percentage, minus/plus buttons and explicit native-form volume submission.
- ✅ Add serialized async fetch refresh/control handling, visibility pause, focus preservation, loading feedback and retry errors.
- ✅ Update theme-aware responsive CSS and the shared English catalog.

## Verification

- ✅ Mocked worker tests cover mixed command/read ordering, disabled access, cancellation, shutdown and failure recovery.
- ✅ Focused Core tests cover command validation, volume bounds and repeat/shuffle mode preservation.
- ✅ Focused cache tests cover reuse between requests, minimum one-hour retention, expiration/invalidation and fresh live state using a mocked gateway and controlled time.
- ✅ Mocked page tests cover antiforgery, invalid commands/IDs, disabled configuration, control rendering, encoding and retry.
- ✅ Mock-host browser checks cover periodic updates, controls, focus, slider dragging, visibility, errors and responsive light/dark layouts.
- ✅ Run dotnet format and verify-no-changes; obtain a zero-warning solution build.
- ✅ Run applicable tests, using direct xUnit executables if the existing solution runner handshake remains broken.
- ✅ Review the full diff and mark all acceptance criteria verified.

## Documentation

- ✅ Update AGENTS.md and Core instructions for authorized browser refresh and playback commands, retaining offline verification and serialization requirements.
- ✅ Update architecture, integrations, backend handlers, frontend, configuration/getting-started and manual-testing documentation plus README.
- ✅ Reconcile goal.md, spec.md and this checklist with the completed implementation.

## Verification limitations

- ⚠️ Virtual tabs in the in-app browser always reported visible. True hidden-tab pause/resume was checked by code inspection; native full-page form handling was exercised by mocked integration tests.
- ✅ Browser operations used a localhost-only stateful fake gateway. The preview host was stopped after verification.

- ✅ Final checks: dotnet format and verify-no-changes pass; the solution build with OutputPath=bin/PlanCheck/ succeeds with zero warnings/errors. All 22 Core and 27 Web tests pass via direct xUnit executables.
- ⚠️ The solution-level Microsoft.Testing.Platform command still exits 5 with runner handshake failures and zero discovered tests; both direct test executables pass. This pre-existing runner issue was not changed.
- ✅ Changes remain unstaged and uncommitted on the current branch; the pinned upstream submodule was not edited or tested.
