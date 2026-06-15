# Bose SoundTouch Bridge — installer build script
# Lefuttatja a dotnet publish-t és az Inno Setup compilert.

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$projectRoot = (Resolve-Path "$scriptDir\..").Path
Set-Location $projectRoot

Write-Host "Project root: $projectRoot" -ForegroundColor Cyan

# Futó példány leállítása (különben az exe le van zárva)
$running = Get-Process -Name BoseSoundTouchBridge -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Stopping running BoseSoundTouchBridge instance(s)..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 800
}

# dotnet
$dotnetCandidates = @(
    "C:\Program Files\dotnet\dotnet.exe",
    "C:\Program Files (x86)\dotnet\dotnet.exe",
    "$env:USERPROFILE\.dotnet\dotnet.exe"
)
$dotnet = $dotnetCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $dotnet) {
    Write-Error ".NET SDK nincs telepítve. Telepítsd: winget install Microsoft.DotNet.SDK.10"
    exit 1
}
Write-Host "dotnet: $dotnet"

# Publish
$publishDir = Join-Path $projectRoot "publish"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Host "`n[1/2] dotnet publish (Release, single-file, self-contained)..." -ForegroundColor Cyan
& $dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false `
    -o $publishDir --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish hiba (exit code $LASTEXITCODE)"
    exit 1
}

$exePath = Join-Path $publishDir "BoseSoundTouchBridge.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "A publish eredménye nem található: $exePath"
    exit 1
}
$exeSize = (Get-Item $exePath).Length / 1MB
Write-Host ("  → BoseSoundTouchBridge.exe ({0:N1} MB)" -f $exeSize) -ForegroundColor Green

# Inno Setup
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    Write-Error @"
Inno Setup nincs telepítve. Telepítsd:
  winget install JRSoftware.InnoSetup
"@
    exit 1
}
Write-Host "Inno Setup: $iscc"

$distDir = Join-Path $projectRoot "dist"
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }

Write-Host "`n[2/2] Inno Setup compile..." -ForegroundColor Cyan
& $iscc /Qp "$scriptDir\setup.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup hiba (exit code $LASTEXITCODE)"
    exit 1
}

$installer = Get-ChildItem $distDir -Filter "*.exe" |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1

if ($installer) {
    $size = $installer.Length / 1MB
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Green
    Write-Host "Installer kész!" -ForegroundColor Green
    Write-Host ("Fájl: {0}" -f $installer.FullName)
    Write-Host ("Méret: {0:N1} MB" -f $size)
    Write-Host "==============================================" -ForegroundColor Green
}
