# Core conventions

Business logic lives in BusinessLogic/ and uses BL suffixes with corresponding I...BL interfaces. Group by subfolder when the domain grows. Put shared configuration in Configuration/ and business limits in BusinessRules; all date calculations accept an injectable DateTime or DateOnly.

ISonosGateway is the sole OpenPhonos boundary. Tests must mock it completely. Never scan for, query or control live speakers during verification. SonosBackgroundService is request-driven; do not add polling, subscriptions, scheduled work or timers.

Keep pages thin; expose application contracts rather than OpenPhonos types. Pass cancellation tokens through async methods. The upstream discovery API cannot cancel in flight; preserve serialization until it completes.

Follow the [root rules](../../AGENTS.md) and [architecture](../../docs/architecture.md).
