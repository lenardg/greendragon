# Backend and page handlers

## SUMMARY

The Web host exposes infrastructure health and an antiforgery-protected Razor Page action. Core owns the request queue and all Sonos interaction.

## HTTP surfaces

| Method and path | Behavior |
|---|---|
| GET / | Renders the portal without discovering speakers |
| POST /?handler=Discover | Validates antiforgery, calls ISonosBL and renders discovered rooms or a retryable error |
| GET /api/health | Returns 200 with an empty body, without checking Sonos |
| GET /api/health?version=true | Returns the Core assembly version as a JSON string |
| GET /health, /alive | Standard development-only Aspire probes |

No application API exists beyond health, so Swagger is not required in this increment. Add future APIs as controllers under Controllers, with request/response models under Models.

## Composition

WebProgram calls only ApplicationSetup for composition. Files in Application isolate configuration, logging, pages, Key Vault and WebSecurity. ApplicationSetup owns the middleware order. GET and health routes never contact the gateway.

SonosServiceCollectionExtensions registers one SonosBackgroundService instance for both hosted execution and business requests. Failures complete only the affected request; the worker continues handling subsequent requests. No scoped dependency is captured by the singleton.

All tests substitute ISonosGateway. Read [architecture](../architecture.md) for queue and cancellation behavior.
