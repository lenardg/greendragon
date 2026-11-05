# Backend and page handlers

## SUMMARY

The Web host exposes infrastructure health and an antiforgery-protected Razor Page action. Core owns the request queue and all Sonos interaction.

## HTTP surfaces

| Method and path | Behavior |
|---|---|
| GET / | Renders the shell; the browser automatically sends a POST when Sonos is enabled |
| POST /?handler=Discover | Validates antiforgery, calls ISonosBL and loads from the shared catalog (or discovers on a miss); rediscover=true explicitly replaces discovery |
| POST /?handler=Refresh | Reads fresh status from the shared catalog without SSDP discovery |
| POST /?handler=Command | Validates speaker ID, command kind and optional 0-100 volume; queues a command and returns updated cards |
| GET /api/health | Returns 200 with an empty body, without checking Sonos |
| GET /api/health?version=true | Returns the Core assembly version as a JSON string |
| GET /health, /alive | Standard development-only Aspire probes |

No application API exists beyond health, so Swagger is not required in this increment. Add future APIs as controllers under Controllers, with request/response models under Models.

## Composition

WebProgram calls only ApplicationSetup for composition. Files in Application isolate configuration, logging, pages, Key Vault and WebSecurity. ApplicationSetup owns the middleware order. GET and health routes never contact the gateway.

SonosServiceCollectionExtensions registers one SonosBackgroundService instance for both hosted execution and business requests. Failures complete only the affected request; the worker continues handling subsequent requests. No scoped dependency is captured by the singleton.

All tests substitute ISonosGateway. Read [architecture](../architecture.md) for queue and cancellation behavior.

Page handlers return the full page for native forms, or card fragments when X-Requested-With is fetch. Every POST enforces antiforgery. Fragments identify success/failure for the client; invalid input and operation failures remain retryable and never expose upstream details. Responses use no-cache/no-store. SpeakerCommandAIM validates bound form fields; SonosBL and the worker validate Core commands before accessing the gateway. IDs resolve only against discovered players; clients never supply network addresses.
