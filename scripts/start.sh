#!/usr/bin/env bash
# Starts Pawnsmith for a try, without Docker (DEC-121): builds the front,
# puts it in the API's wwwroot, and runs the API on http://127.0.0.1:8080.
#
# Data live in data/ at the root of the repository: projects, logs, and the
# user directory (personal catalogue and styles, generator address, imported
# workflow). Configuration is read from config/.
#
# Needs the .NET 10 SDK and Node 22. Stop with Ctrl+C.
set -euo pipefail

cd "$(dirname "$0")/.."

echo "== Building the front"
(cd src/Pawnsmith.Web && npm ci --no-audit --no-fund && npm run build)

echo "== Copying it into the API's wwwroot"
# Everything but .gitkeep, which keeps the folder in git: a previous build
# must not leave its old bundle behind.
find src/Pawnsmith.Api/wwwroot -mindepth 1 ! -name .gitkeep -exec rm -rf {} +
cp -R src/Pawnsmith.Web/dist/. src/Pawnsmith.Api/wwwroot/

# ComfyUI's usual address on the same machine, unless one is given. An
# address saved from the Generator page wins over this one (DEC-108).
export Pawnsmith__Generator__Url="${Pawnsmith__Generator__Url:-http://127.0.0.1:8188}"

echo "== Starting Pawnsmith on http://127.0.0.1:8080 (Ctrl+C to stop)"
# The "http" profile of launchSettings.json: Development, so that
# appsettings.Development.json points the data at the repository's data/.
exec dotnet run --project src/Pawnsmith.Api --configuration Release --launch-profile http
