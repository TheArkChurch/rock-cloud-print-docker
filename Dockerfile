# syntax=docker/dockerfile:1
# Pin the build stage to linux/amd64 so dotnet always runs natively.
# The published DLLs are platform-agnostic MSIL and run on both amd64 and arm64
# without any QEMU emulation. The final runtime stage still uses the correct
# platform-specific ASP.NET image for whichever architecture is being built.
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so dotnet restore is cached independently of source changes.
COPY Rock.CloudPrint.Service/Rock.CloudPrint.Service.csproj     Rock.CloudPrint.Service/
COPY Rock.CloudPrint.Shared/Rock.CloudPrint.Shared.csproj        Rock.CloudPrint.Shared/
COPY Rock.CloudPrint.Shared.Common/Rock.CloudPrint.Shared.Common.projitems  Rock.CloudPrint.Shared.Common/
COPY Rock.CloudPrint.Shared.Common/Rock.CloudPrint.Shared.Common.shproj     Rock.CloudPrint.Shared.Common/

RUN dotnet restore Rock.CloudPrint.Service/Rock.CloudPrint.Service.csproj

# Copy source and publish.
COPY Rock.CloudPrint.Service/      Rock.CloudPrint.Service/
COPY Rock.CloudPrint.Shared/       Rock.CloudPrint.Shared/
COPY Rock.CloudPrint.Shared.Common/ Rock.CloudPrint.Shared.Common/

RUN dotnet publish Rock.CloudPrint.Service/Rock.CloudPrint.Service.csproj \
    -c Release \
    -o /app/publish

# ── Runtime image ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Pre-create the config directory. When operators bind-mount a host directory
# here (e.g. ./config or a TrueNAS dataset) Docker uses this as the mount
# point. Without it Docker would create the directory as root, which can cause
# permission issues on some hosts.
RUN mkdir -p /app/config

# Port the web UI listens on (also set in appsettings.json "Urls").
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rock.CloudPrint.Service.dll"]
