# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# A.6 — Single image, three stages.
#
#   1. Node   : build the Vite bundle of Pawnsmith.Web
#   2. .NET   : restore, build and publish Pawnsmith.Api
#   3. runtime: ASP.NET Core image, binary + front bundle in wwwroot
#
# The API serves the front from the same origin, so there is no CORS setup.
# tools/Pawnsmith.Cli is a throwaway harness (B.7) and is deliberately never
# copied into any stage.
# ---------------------------------------------------------------------------


# --- Stage 1: front -------------------------------------------------------
FROM node:22-alpine AS front

WORKDIR /src/web

# Manifests first: this layer is only invalidated when the dependency set
# changes, not on every source edit.
COPY src/Pawnsmith.Web/package.json src/Pawnsmith.Web/package-lock.json ./
RUN npm ci

COPY src/Pawnsmith.Web/ ./
RUN npm run build


# --- Stage 2: API ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend

WORKDIR /src

# Same reasoning as above: project files first, so that `restore` is cached
# independently of the C# sources. Directory.Build.props is part of the
# evaluation of every project and must be present before restore.
COPY Directory.Build.props ./
COPY src/Pawnsmith.Domain/Pawnsmith.Domain.csproj                 src/Pawnsmith.Domain/
COPY src/Pawnsmith.Application/Pawnsmith.Application.csproj       src/Pawnsmith.Application/
COPY src/Pawnsmith.Infrastructure/Pawnsmith.Infrastructure.csproj src/Pawnsmith.Infrastructure/
COPY src/Pawnsmith.Api/Pawnsmith.Api.csproj                       src/Pawnsmith.Api/
RUN dotnet restore src/Pawnsmith.Api/Pawnsmith.Api.csproj

COPY .editorconfig ./
COPY src/Pawnsmith.Domain/         src/Pawnsmith.Domain/
COPY src/Pawnsmith.Application/    src/Pawnsmith.Application/
COPY src/Pawnsmith.Infrastructure/ src/Pawnsmith.Infrastructure/
COPY src/Pawnsmith.Api/            src/Pawnsmith.Api/

RUN dotnet publish src/Pawnsmith.Api/Pawnsmith.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish


# --- Stage 3: runtime -----------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=backend /app/publish ./
COPY --from=front   /src/web/dist ./wwwroot/

# Physical values live outside the binary and are meant to be overridden by a
# bind mount after the T0 control print (B.2).
COPY config/ ./config/

# DEC-022 — projects and logs are two distinct volumes: a shared project archive
# must never carry prompts, absolute paths or the generator URL.
VOLUME ["/app/data/projects", "/app/data/logs"]

# MEN-004 — the application has no authentication. The canonical run form binds
# the published port to the loopback interface only:
#   docker run -p 127.0.0.1:8080:8080 pawnsmith
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Pawnsmith.Api.dll"]
