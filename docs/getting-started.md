# Getting started

## SUMMARY

Restore the pinned dependency, restore tools and start the portal locally. Enable Sonos access only for normal use on your home network.

## Prerequisites

.NET 10 SDK (global.json rolls forward within .NET 10), Aspire CLI 13.6.4, Git and an HTTPS development certificate. Podman is the preferred Aspire container runtime; this solution currently defines no container resources.

Clone with --recurse-submodules, or initialize the pinned source after cloning:

```powershell
git submodule update --init --recursive
dotnet tool restore
dotnet restore src/GreenDragon.slnx
dotnet dev-certs https --trust
aspire run --apphost src/GreenDragon.AppHost/GreenDragon.AppHost.csproj
```

The portal listens on HTTP 62001 and HTTPS 62002 through its launch profile. Aspire's dashboard URL is printed by the CLI. To run Web directly:

```powershell
dotnet run --project src/GreenDragon.Web --launch-profile https
```

## Local settings

The host's user secrets ID is GreenDragon-web. No secrets are required for the initial scaffold. Keep local overrides in src/GreenDragon.Web/appsettings.local.json (gitignored), or set user secrets:

```powershell
dotnet user-secrets set "Sonos:Enabled" "true" --project src/GreenDragon.Web
```

When access is enabled, opening the portal loads speaker cards and refreshes status periodically while visible. Select Refresh speakers to explicitly rediscover devices after catalog expiry or network changes. With JavaScript disabled, the same button loads speakers, and native forms handle playback/modes and Apply volume. Leave access disabled for automated smoke tests. Automated tests replace ISonosGateway even when testing the enabled path.

For access from another device, configure Kestrel binding, AllowedHosts and a trusted HTTPS endpoint for the chosen home-network hostname. Checked-in defaults accept localhost and 127.0.0.1 only. Do not expose the anonymous portal to the public internet.

## Developer checks

```powershell
dotnet format src/GreenDragon.slnx --exclude external
dotnet format src/GreenDragon.slnx --exclude external --verify-no-changes
dotnet build src/GreenDragon.slnx
dotnet test --solution src/GreenDragon.slnx
dotnet outdated src/GreenDragon.slnx --pre-release Never --fail-on-updates
dotnet list src/GreenDragon.slnx package --vulnerable --include-transitive
```

See [backend details](backend/api.md) and [manual testing](manual-testing.md).

## Port conflicts

If another local application already uses 62001 or 62002, start Aspire with --isolated to choose separate ports and use the dashboard's GreenDragon endpoint. Do not stop unrelated applications. The default launch ports are 62001/62002.

```powershell
aspire run --apphost src/GreenDragon.AppHost/GreenDragon.AppHost.csproj --isolated
```

Refresh cadence and the minimum one-hour metadata lifetime can be configured with Sonos:RefreshIntervalSeconds and Sonos:MetadataCacheDurationMinutes; defaults and validation are listed in [environment](environment.md#sonos-refresh-and-retention-settings). The stable catalog is shared across browser requests in the host process; playback values remain fresh.
