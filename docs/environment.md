# Environment

## SUMMARY

Development and home deployment both run within the home network. Configuration precedence allows private machine-specific settings.

## Environments

| Environment | Behavior |
|---|---|
| Development | HTTPS development certificate, Aspire dashboard, /health and /alive probes; anonymous portal |
| Home deployment | Host reachable by Sonos devices on the LAN, trusted HTTPS endpoint, production HSTS; no development probes |

The application always exposes /api/health without accessing Sonos.

Configuration precedence, lowest to highest: appsettings.json, appsettings.{Environment}.json, appsettings.local.json, Development user secrets, environment variables, command line, optional Key Vault.

Sonos:Enabled defaults to false. Enable explicitly for normal use. No speaker addresses, credentials or runtime state are committed. AllowedHosts defaults to localhost; configure the intended LAN hostname when deploying. Configure trusted proxy addresses if TLS is terminated at a reverse proxy; never trust forwarded headers from arbitrary clients.

Optional Key Vault uses ConnectionStrings:GreenDragon-keyvault and DefaultAzureCredential. It is not contacted unless configured. Optional Application Insights uses APPLICATIONINSIGHTS_CONNECTION_STRING; otherwise Aspire can use OTEL_EXPORTER_OTLP_ENDPOINT.

Keep the anonymous portal on a trusted private network. Cloud hosting would require a separately designed private network connection to Sonos and is not part of this scaffold.
