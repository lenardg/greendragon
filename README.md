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

## Linux and Raspberry Pi containers

The root Dockerfile builds the Web host directly, without Aspire. It uses .NET 10 Ubuntu images and a non-root runtime user. Use a 64-bit Linux OS on Raspberry Pi (for example, 64-bit Raspberry Pi OS on a Pi 3, 4 or 5); the same Dockerfile also supports Linux x64.

Run these commands from the repository root. The pinned OpenPhonos source must be present in the build context:

```sh
git submodule update --init --recursive
docker buildx build --platform linux/arm64 --load -t greendragon:local .
```

For an x64 Linux host, replace `linux/arm64` with `linux/amd64`. A build on another computer produces an image for the selected target; transfer it with `docker save` / `docker load`, or push it to your registry. Plain `docker build -t greendragon:local .` on the Pi selects its native architecture.

Production requires HTTPS for secure antiforgery cookies and form submissions. Provide a password-protected PFX certificate trusted by your browsers and valid for your chosen LAN hostname. Keep certificates outside the repository and image, and make the mounted file readable by the container's `app` user (UID 1654). For example, on the Linux host, with a certificate at `/srv/greendragon/certs/portal.pfx` and hostname `greendragon.home`:

```sh
# Set this to the certificate password in your shell before running Docker.
export ASPNETCORE_Kestrel__Certificates__Default__Password='replace-with-your-certificate-password'
docker run -d --name greendragon --restart unless-stopped \
  --network host \
  --mount type=bind,src=/srv/greendragon/certs,dst=/https,readonly \
  --env ASPNETCORE_Kestrel__Certificates__Default__Path=/https/portal.pfx \
  --env ASPNETCORE_Kestrel__Certificates__Default__Password \
  --env ASPNETCORE_HTTPS_PORT=8443 \
  --env 'AllowedHosts=greendragon.home;localhost;127.0.0.1' \
  --env Sonos__Enabled=false \
  greendragon:local
```

Open `https://greendragon.home:8443`; HTTP on port 8080 redirects there. Adjust the hostname, certificate path and ports for your host. The HTTPS certificate is required at startup with the image's default listeners. Override `ASPNETCORE_HTTP_PORTS`, `ASPNETCORE_HTTPS_PORTS` and `ASPNETCORE_HTTPS_PORT` together when changing ports.

Linux host networking lets request-driven Sonos UPnP discovery use the host's LAN interfaces. It does not use Docker `-p` port mappings: ports 8080 and 8443 must be free on the host. Keep the host on the speakers' network and permit the required LAN traffic through its firewall. Sonos stays disabled by default; for normal speaker use, recreate the container with `Sonos__Enabled=true`. Never enable Sonos during automated verification. The anonymous portal belongs on your trusted LAN.

See [Docker host networking](https://docs.docker.com/engine/network/drivers/host/) and the [.NET multi-platform build guidance](https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/) for the underlying container options.
