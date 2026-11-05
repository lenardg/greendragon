# The Green Dragon

A small Razor Pages portal for a home Sonos system, using [OpenPhonos](https://github.com/amp64/openphonos).

The initial scaffold wires a request-driven hosted worker and explicit speaker discovery. Now-playing views and shortcuts, including white noise, are the next increment.

```powershell
git submodule update --init --recursive
dotnet tool restore
aspire run --apphost src/GreenDragon.AppHost/GreenDragon.AppHost.csproj
```

Open [the local portal](https://localhost:60443). Sonos access is disabled by default. See [getting started](docs/getting-started.md) to enable it for normal use. Tests and automated verification must never contact real speakers.

See the [documentation index](docs/AGENTS.md) for architecture, configuration and verification.
