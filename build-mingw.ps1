# Builds the Blazma overlay core (BlazmaOverlay.exe).
# Use build.ps1 to build the overlay and the Avalonia settings UI together.

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$c = Get-ChildItem -Path "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter 'g++.exe' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $c) {
    $c = Get-ChildItem -Path 'C:\mingw64\bin', 'C:\msys64\mingw64\bin', 'C:\Program Files\LLVM-MinGW\bin', 'C:\Program Files (x86)\LLVM-MinGW\bin' -Filter 'g++.exe' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
}

if (-not $c) {
    Write-Host 'No MinGW/Clang compiler found.'
    Write-Host 'Install LLVM-MinGW with: winget install --id MartinStorsjo.LLVM-MinGW.UCRT'
    exit 1
}

Write-Host "Using $c"

$bin = Split-Path $c -Parent
$windres = Join-Path $bin 'windres.exe'

# Icon, manifest and version metadata.
$res = Join-Path $env:TEMP 'blazma-res.o'
if (Test-Path $windres) {
    & $windres -i 'src/Blazma.rc' -o $res -O coff
    if ($LASTEXITCODE -ne 0) { Write-Host 'Resource compilation failed.'; exit 1 }
} else {
    Write-Host 'windres not found; building without icon and version info.'
    $res = $null
}

$sources = @('src/main.cpp', 'src/Config.cpp', 'src/CrosshairRenderer.cpp')
if ($res) { $sources += $res }

# -static bundles the C++ runtime into the exe. Without it the binary depends on
# libc++.dll from the toolchain, which is not present on a normal Windows install.
$flags = @(
    '-std=c++17', '-O2', '-DUNICODE', '-D_UNICODE',
    '-mwindows', '-municode', '-static',
    '-o', 'BlazmaOverlay.exe'
)

# The settings dialog now lives in the Avalonia UI, so the common-controls,
# common-dialog and DWM libraries the old in-process dialog needed are gone.
$libs = @('-lgdi32', '-lgdiplus', '-lshell32', '-luser32')

& $c ($flags + $sources + $libs)

if ($LASTEXITCODE -ne 0) {
    Write-Host 'Build failed.'
    exit 1
}

Write-Host 'Build succeeded: BlazmaOverlay.exe'
