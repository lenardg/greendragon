# Core conventions

Business logic lives in BusinessLogic/ and uses BL suffixes with corresponding I...BL interfaces. Group by subfolder when the domain grows. Put shared configuration in Configuration/ and business limits in BusinessRules; all date calculations accept an injectable DateTime or DateOnly.

ISonosGateway is the sole OpenPhonos boundary. Tests must mock it completely. Never scan for, query or control live speakers during verification. SonosBackgroundService is request-driven and serializes discovery, browser refresh and commands in one bounded queue. Do not add server polling, subscriptions, scheduled work or timers. Use ISonosMetadataCache/IMemoryCache with SonosCachePolicy.PlayerCatalog for 24-hour sliding player-ID/catalog retention. Other stable metadata keeps at least one-hour absolute expiration; keep live state fresh. Only the gateway accesses cached upstream objects, and only from the worker.

Keep pages thin; expose application contracts rather than OpenPhonos types. Pass cancellation tokens through async methods. The upstream discovery API cannot cancel in flight; preserve serialization until it completes.

Follow the [root rules](../../AGENTS.md) and [architecture](../../docs/architecture.md).
