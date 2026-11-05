# Speaker controls - Goal

## Goal

Turn the home-page speaker cards into compact, automatically refreshed playback controls. Users can see the current track and control playback, repeat, shuffle and room volume without reloading the page.

## Background

The current cards show read-only snapshots with large artwork and the model name. The user supplied a screenshot and requested smaller artwork, the track title above it, live status and interactive controls.

## Scope

- Remove the speaker model from the card and put the track title above smaller artwork.
- Refresh visible cards using asynchronous fetch requests approximately every five seconds.
- Add accessible SVG Previous, Play/Pause, Next, repeat and shuffle buttons.
- Add an adjustable volume slider with small minus/plus buttons and a thicker track.
- Preserve native form handling, shared localization, themes, antiforgery and the single bounded Sonos operation queue.
- Share player IDs and player information between requests through ASP.NET Core MemoryCache, with a minimum one-hour configured lifetime. Apply the same caching policy to favorites, playlists and music-library results when their retrieval is introduced.
- Verify exclusively with a mocked ISonosGateway.

Shortcuts, seeking, grouping controls, event subscriptions, server background polling and persistent storage are outside scope.

## Success

Enabled portals show compact, current cards and working controls. Requests never overlap inside the Sonos gateway, disabled portals never access Sonos, and errors remain retryable. Tests and browser verification do not contact real devices.
