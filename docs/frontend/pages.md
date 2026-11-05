# Portal UI

## SUMMARY

The initial responsive Razor page discovers visible rooms on demand. Now-playing details and shortcut activation are future work.

## Starter page

Razor renders the product heading, description and Discover speakers form. Results appear as room cards after a successful POST; an empty result and an error have separate messages. No initial request contacts Sonos. JavaScript disables duplicate submissions and announces loading, while the native form also works without JavaScript.

The page uses semantic elements, visible keyboard focus and flexible sizing for phones, tablets and desktops. Room names are encoded by Razor. CSS variables support system, light and dark themes; explicit theme selection is saved in browser local storage when available.

The shared English JSON catalog is wwwroot/i18n/en.json. Razor reads it through UiText; client-only code can load the same catalog later. Browser language preferences fall back to English, the only supported language. Current JavaScript uses localized data attributes.

Typography loads Roboto and Russo One from Google Fonts with system fallbacks. CSP allows only those external font origins. No frontend package manager or UI framework is needed.

See [manual testing](../manual-testing.md).
