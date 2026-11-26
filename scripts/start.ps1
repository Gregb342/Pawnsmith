# Starts Pawnsmith for a try, without Docker (DEC-121): builds the front,
# puts it in the API's wwwroot, and runs the API on http://127.0.0.1:8080.
#
# Data live in data/ at the root of the repository: projects, logs, and the
# user directory (personal catalogue and styles, generator address, imported
# workflow). Configuration is read from config/.
#
# Needs the .NET 10 SDK and Node 22. Run from PowerShell:
#   powershell -ExecutionPolicy Bypass -File scripts\start.ps1
# Stop with Ctrl+C.

$ErrorActionPreference = 'Stop'

# Paths use forward slashes: Windows PowerShell, PowerShell 7 on any system,
# and dotnet all accept them, where dotnet on Linux or macOS reads a
# backslash as part of a file name. The script then runs, unchanged, on
# every system PowerShell runs on.

Set-Location (Join-Path $PSScriptRoot '..')

Write-Host '== Building the front'
Push-Location 'src/Pawnsmith.Web'
try {
    npm ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed ($LASTEXITCODE)" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed ($LASTEXITCODE)" }
}
finally {
    Pop-Location
}

Write-Host "== Copying it into the API's wwwroot"
# Everything but .gitkeep, which keeps the folder in git: a previous build
# must not leave its old bundle behind.
Get-ChildItem 'src/Pawnsmith.Api/wwwroot' -Force |
    Where-Object { $_.Name -ne '.gitkeep' } |
    Remove-Item -Recurse -Force
Copy-Item -Path 'src/Pawnsmith.Web/dist/*' -Destination 'src/Pawnsmith.Api/wwwroot' -Recurse

# ComfyUI's usual address on the same machine, unless one is given. An
# address saved from the Generator page wins over this one (DEC-108). The
# desktop edition of ComfyUI listens on port 8000 instead: change it there.
if (-not $env:Pawnsmith__Generator__Url) {
    $env:Pawnsmith__Generator__Url = 'http://127.0.0.1:8188'
}

Write-Host '== Starting Pawnsmith on http://127.0.0.1:8080 (Ctrl+C to stop)'
# The "http" profile of launchSettings.json: Development, so that
# appsettings.Development.json points the data at the repository's data/.
dotnet run --project 'src/Pawnsmith.Api' --configuration Release --launch-profile http
