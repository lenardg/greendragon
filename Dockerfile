# syntax=docker/dockerfile:1

# Build on the builder's CPU; publish for the requested Linux architecture.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG TARGETARCH
WORKDIR /source
COPY . .
RUN test -f external/openphonos/src/UPnP/UPnP.resx \
    || (echo "Initialize the pinned submodule: git submodule update --init --recursive" >&2; exit 1)
RUN dotnet publish src/GreenDragon.Web/GreenDragon.Web.csproj \
    --configuration Release --os linux --arch $TARGETARCH \
    --self-contained false --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_HTTPS_PORTS=8443
EXPOSE 8080 8443
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "GreenDragon.Web.dll"]
