# Portal UI

## SUMMARY

The responsive Razor page shows read-only speaker snapshots. Playback controls and shortcut activation are future work.

## Speaker overview

Razor renders the shell without contacting Sonos. When Sonos is enabled, JavaScript sends one antiforgery-protected POST automatically and updates the results in place. Refresh speakers repeats the request; there is no polling or progress timer. Without JavaScript the native form loads the same results. Disabled configuration shows an explanatory message and sends no requests. Loading is announced accessibly and duplicate submissions are blocked. Errors allow retry; an empty result has its own message.

The page uses semantic elements, visible keyboard focus and flexible sizing for phones, tablets and desktops. Room names, titles, artists and albums are encoded by Razor. Cards show artwork or a placeholder, transport state, elapsed time and duration, volume and mute. Unsupported values display Unavailable. Group members share coordinator playback but retain their own volume/mute. The view clearly labels its values as a snapshot. CSS variables support system, light and dark themes; explicit theme selection is saved in browser local storage when available.

The shared English JSON catalog is wwwroot/i18n/en.json. Razor reads it through UiText; client-only code can load the same catalog later. Browser language preferences fall back to English, the only supported language. Current JavaScript uses localized data attributes.

Typography loads Roboto and Russo One from Google Fonts with system fallbacks. CSP allows those external font origins and embedded raster artwork. No frontend package manager or UI framework is needed.

See [manual testing](../manual-testing.md).
