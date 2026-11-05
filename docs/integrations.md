# Integrations

## SUMMARY

OpenPhonos provides local Sonos discovery and future playback control. All automated tests replace its application boundary.

## OpenPhonos

Source: [amp64/openphonos](https://github.com/amp64/openphonos), MIT license retained at external/openphonos/LICENSE. The Git submodule pins the exact commit. There is no OpenPhonos NuGet package. Use git submodule status to inspect the revision.

The two source-build projects under src compile the unmodified Sonos and UPnP sources for .NET 10. Their original project files are not used: System.Text.Json comes from the shared runtime, and Logging.Abstractions uses the solution's current centrally managed version. This avoids inheriting obsolete dependency versions or prerelease package identities.

Upstream sources are exempt from application formatting, XML documentation and nullable requirements. Only the Sonos wrapper suppresses the upstream unused-local warnings CS0168 and CS0219. No application warning is suppressed. Do not format or rewrite the submodule as part of application changes.

ISonosGateway is the single replaceable network boundary. OpenPhonosGateway currently maps explicit discovery into SpeakerSnapshot records. SonosPlatform supplies server platform behavior, and library logs use the configured application ILoggerFactory. Discovery exceptions are logged and translated to safe portal errors.

The application does not create event subscriptions, run playback commands or schedule work in this increment. Add new command behavior through Core business logic and the same serialized worker.

## Maintenance and verification

Update the submodule deliberately to a reviewed commit, then rebuild the wrappers and run all mocked tests. Do not run upstream test projects, SSDP scans, playback commands or any integration test against live Sonos devices. A successful mocked test verifies our wiring, not the underlying Sonos protocol.
