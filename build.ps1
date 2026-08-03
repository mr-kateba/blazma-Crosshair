# Builds both halves of Blazma and lays them out in dist/.
#
#   dist/
#     Blazma.exe            settings UI (self-contained) - the app you launch
#     BlazmaOverlay.exe     native overlay renderer (no dependencies)
#
# Settings live in %APPDATA%\Blazma\config.ini. Dropping a config.ini into dist/
# switches the pair to portable mode and keeps settings alongside the binaries.
#
# Usage:  .\build.ps1            build everything
#         .\build.ps1 -CoreOnly  skip the .NET UI
#         .\build.ps1 -Portable  also seed dist/config.ini

param(
    [switch]$CoreOnly,
    [switch]$Portable
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

# A running overlay or UI would lock the exe we are about to overwrite.
Get-Process Blazma, BlazmaOverlay -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

Write-Host '== Overlay core (C++) ==' -ForegroundColor Cyan
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'build-mingw.ps1')
if ($LASTEXITCODE -ne 0) { exit 1 }
Copy-Item 'BlazmaOverlay.exe' $dist -Force

if (-not $CoreOnly) {
    Write-Host ''
    Write-Host '== Settings UI (Avalonia) ==' -ForegroundColor Cyan

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host 'dotnet SDK not found. Install with: winget install --id Microsoft.DotNet.SDK.10'
        exit 1
    }

    dotnet publish (Join-Path $PSScriptRoot 'ui\Blazma.csproj') `
        -c Release -o $dist --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Host 'UI build failed.'; exit 1 }

    # Skia and HarfBuzz ship native symbol files as runtime assets; they are ~100 MB
    # and serve no purpose in a release drop.
    Get-ChildItem $dist -Filter '*.pdb' | Remove-Item -Force
}

if ($Portable) {
    $distConfig = Join-Path $dist 'config.ini'
    if (-not (Test-Path $distConfig)) { Copy-Item 'config.ini' $distConfig }
}

Write-Host ''
Write-Host "Done. Output in $dist" -ForegroundColor Green
Get-ChildItem $dist -Filter '*.exe' | ForEach-Object {
    '{0,-24} {1,8:N0} KB' -f $_.Name, ($_.Length / 1KB)
}
