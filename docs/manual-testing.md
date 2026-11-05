# Manual testing

## SUMMARY

Verify the portal without live Sonos devices. Enabled-path tests require a fully mocked ISonosGateway.

## Scope and setup

Run the Web host with Sonos:Enabled=false and without configured cloud services. Never enable or test against live Sonos during verification. For enabled-path browser checks, use a host that replaces ISonosGateway completely. Playback controls and shortcuts are outside scope.

## Cases

| ID | Case | Steps | Expected result | Reference | Result |
|---|---|---|---|---|---|
| UI-01 | Starter | Open the portal | Product heading and Sonos disabled message are visible | [UI](frontend/pages.md) | |
| UI-02 | Responsive | View at phone, tablet and desktop widths | No horizontal overflow; controls remain usable | [UI](frontend/pages.md) | |
| UI-03 | Themes | Choose Light, Dark and System; reload | Readable colors and saved selection | [UI](frontend/pages.md) | |
| UI-04 | Disabled discovery | Open the portal with access disabled | No refresh form or automatic speaker request; no device access | [Backend](backend/api.md) | |
| UI-05 | Keyboard | Tab through select, button and attribution link | Logical order, accessible names and visible focus | [UI](frontend/pages.md) | |
| UI-06 | Unsupported language | Set an unsupported browser language and reload | English fallback renders | [UI](frontend/pages.md) | |
| UI-07 | Automatic overview (mock host) | Open the enabled portal backed by ISonosGateway fake data | All supplied room cards load automatically with cover art, playback status/position and volume/mute | [UI](frontend/pages.md) | |
| UI-08 | Refresh (mock host) | Select Refresh speakers | Results update in place; the button becomes usable again | [UI](frontend/pages.md) | |
| UI-09 | Missing data (mock host) | Supply paused, unknown and missing-artwork snapshots | Accurate state and Unavailable labels; no fabricated position or volume | [UI](frontend/pages.md) | |
| UI-10 | Retry (mock host) | Return a gateway failure, then retry successfully | Readable error, usable refresh action, successful recovery | [Backend](backend/api.md) | |

## Execution log

Record environment, date, operator and results here when performing a manual pass.

2026-11-05: Codex browser pass against the direct GreenDragon Aspire endpoint with Sonos disabled. UI-01 through UI-04 passed at 390x844, 768x1024 and 1280x800. Light/Dark/System, visible focus on the theme selector, persistence after reload and retry after disabled discovery were checked. Full keyboard traversal and browser-language preference switching were not performed. The standard HTTPS port was occupied by another local app; its process was left running and only the GreenDragon smoke AppHost was stopped afterward.

2026-11-05: Mocked Kestrel browser pass verified automatic loading and refresh with three fake rooms, including artwork, playing/paused states, volume/mute and unavailable values. Layout was checked at 390x844, 768x1024 and 1280x1000 in system dark and explicit light themes. Integration tests verified retry, empty results, metadata encoding and disabled configuration. No live Sonos access. Full keyboard traversal and slow-request timing were not manually exercised.
