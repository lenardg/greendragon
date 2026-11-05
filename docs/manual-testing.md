# Manual testing

## SUMMARY

Verify the scaffold UI without live Sonos devices. Automated tests use a mocked Sonos gateway for the successful discovery path.

## Scope and setup

Run the Web host with Sonos:Enabled=false and without configured cloud services. Never enable or test against live Sonos during verification. Playback, now-playing metadata and shortcuts are outside this increment.

## Cases

| ID | Case | Steps | Expected result | Reference | Result |
|---|---|---|---|---|---|
| UI-01 | Starter | Open the portal | Product heading and Discover speakers action are visible | [UI](frontend/pages.md) | |
| UI-02 | Responsive | View at phone, tablet and desktop widths | No horizontal overflow; controls remain usable | [UI](frontend/pages.md) | |
| UI-03 | Themes | Choose Light, Dark and System; reload | Readable colors and saved selection | [UI](frontend/pages.md) | |
| UI-04 | Disabled discovery | Press Discover speakers with access disabled | A readable error appears and the action is retryable; no device access | [Backend](backend/api.md) | |
| UI-05 | Keyboard | Tab through select, button and attribution link | Logical order, accessible names and visible focus | [UI](frontend/pages.md) | |
| UI-06 | Unsupported language | Set an unsupported browser language and reload | English fallback renders | [UI](frontend/pages.md) | |

## Execution log

Record environment, date, operator and results here when performing a manual pass.

2026-11-05: Codex browser pass against the direct GreenDragon Aspire endpoint with Sonos disabled. UI-01 through UI-04 passed at 390x844, 768x1024 and 1280x800. Light/Dark/System, visible focus on the theme selector, persistence after reload and retry after disabled discovery were checked. Full keyboard traversal and browser-language preference switching were not performed. The standard HTTPS port was occupied by another local app; its process was left running and only the GreenDragon smoke AppHost was stopped afterward.
