# The Green Dragon

A small Razor Pages portal for a home Sonos system, using [OpenPhonos](https://github.com/amp64/openphonos).

When Sonos is enabled, compact speaker cards update every five seconds and offer Previous, Play/Pause, Next, repeat/shuffle and room volume controls. Player IDs and their catalog are shared through ASP.NET Core MemoryCache with 24-hour sliding expiration. Shortcuts remain future work.

```powershell
git submodule update --init --recursive
dotnet tool restore
aspire run --apphost src/GreenDragon.AppHost/GreenDragon.AppHost.csproj
```

Open [the local portal](https://localhost:62002). Sonos access is disabled by default. See [getting started](docs/getting-started.md) to enable it for normal use. Tests and automated verification must never contact real speakers.

See the [documentation index](docs/AGENTS.md) for architecture, configuration and verification.
