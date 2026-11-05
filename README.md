# The Green Dragon

A small Razor Pages portal for a home Sonos system, using [OpenPhonos](https://github.com/amp64/openphonos).

The portal automatically loads a read-only speaker overview when Sonos is enabled, with cover art, playback status and position, and room volume/mute. Refresh speakers updates the snapshot on demand. Playback controls and shortcuts remain future work.

```powershell
git submodule update --init --recursive
dotnet tool restore
aspire run --apphost src/GreenDragon.AppHost/GreenDragon.AppHost.csproj
```

Open [the local portal](https://localhost:62002). Sonos access is disabled by default. See [getting started](docs/getting-started.md) to enable it for normal use. Tests and automated verification must never contact real speakers.

See the [documentation index](docs/AGENTS.md) for architecture, configuration and verification.
