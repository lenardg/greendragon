# Portal UI

## SUMMARY

The responsive Razor portal shows live speaker cards with playback, repeat/shuffle and room volume controls. Shortcuts remain future work.

## Speaker overview

The initial GET renders a shell without accessing Sonos. When enabled, JavaScript loads cards through an antiforgery-protected POST, then uses async fetch to read fresh state about five seconds after each completed request. It pauses scheduled reads while hidden or editing volume. No request is started to catch up with a slow response, and initial discovery failures require explicit retry instead of recurring scans.

Refresh speakers explicitly rediscovers devices, including after cache expiration or a network change. Normal status refresh reuses the shared catalog described in [integrations](../integrations.md). Disabled configuration shows an explanation and sends no requests.

Cards omit the model, show the title/artist/album above artwork limited to 10rem square, and display current state, elapsed time/duration and mute. Missing metadata/artwork has an explicit fallback. Razor encodes untrusted metadata.

## Controls and updates

- Previous, Play/Pause and Next target the room's current group coordinator. Unsupported source operations remain disabled.
- Repeat cycles Off, All and One; shuffle toggles independently. SVG icons, text, accessible names and pressed states expose the settings.
- Volume targets the selected room. The thick range slider accepts 0-100, and minus/plus adjust one point. Slider changes submit when committed rather than on every pointer movement. Boundary buttons disable at 0 and 100.
- Without JavaScript, native antiforgery-protected forms submit all controls; Apply volume commits the range input. JavaScript normally hides that extra button and reveals it for a failed slider submission.
- Fetch requests receive server-rendered fragments. Stable control nodes keep focus; status updates cannot replace a range input during editing or overwrite a newer command result. Loading disables duplicate commands and is announced accessibly. Failed mutations are never automatically replayed; retry is explicit. Failed reads retain the existing cards and show an error.

The server processes every read and command through one bounded queue. Browser cancellation stops the caller's wait, not an unfinished upstream action. See [backend handlers](../backend/api.md).

## Presentation

The compact site header contains the product title and theme selector. The page has no introductory hero, so speaker content starts directly below the header. Semantic HTML, keyboard focus and flexible layouts support phones, tablets and desktops. CSS variables support System, Light and Dark; explicit selection is retained in browser local storage. Reduced-motion preferences disable spinner rotation.

The shared English catalog is wwwroot/i18n/en.json, read by UiText and supplied to JavaScript through data attributes. Unsupported browser cultures fall back to English. Roboto and Uncial Antiqua load from Google Fonts with system/serif fallbacks; Uncial Antiqua is used for the product title. CSP permits the font origins and embedded raster artwork. No frontend package manager or UI framework is required.

See [manual testing](../manual-testing.md).
