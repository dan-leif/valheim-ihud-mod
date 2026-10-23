# Test launcher: builds and installs iHUD, installs the test-only DevAutoLoad plugin,
# arms a one-shot menu skip, and launches Valheim through Steam straight into
# Dove / Reborn.
#   powershell -ExecutionPolicy Bypass -File local\dev-test.ps1
#   powershell -ExecutionPolicy Bypass -File local\dev-test.ps1 -Cleanup   # remove DevAutoLoad again
param(
    [switch]$Cleanup,
    [string]$Character = "Dove",
    [string]$World = "Reborn"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$profileBepInEx = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Ashlands Reborn\BepInEx"
$pluginDir = "$profileBepInEx\plugins\iHUD-DevAutoLoad"
$flag = "$profileBepInEx\ihud-devautoload.flag"

if (Get-Process valheim -ErrorAction SilentlyContinue) {
    throw "Valheim is running. Close it first (it locks the DLLs)."
}

if ($Cleanup) {
    Remove-Item $flag -ErrorAction SilentlyContinue
    if (Test-Path $pluginDir) { Remove-Item $pluginDir -Recurse -Force }
    Write-Host "DevAutoLoad removed from the profile."
    exit 0
}

dotnet build -c Release "$root\local\iHUD.local.csproj"
if ($LASTEXITCODE -ne 0) { throw "iHUD build failed" }

dotnet build -c Release "$root\local\DevAutoLoad\DevAutoLoad.csproj"
if ($LASTEXITCODE -ne 0) { throw "DevAutoLoad build failed" }

New-Item -ItemType Directory -Force $pluginDir | Out-Null
Copy-Item "$root\local\DevAutoLoad\bin\Release\iHUD.DevAutoLoad.dll" $pluginDir -Force

Set-Content -Path $flag -Value "$Character|$World" -NoNewline
Write-Host "Armed one-shot autoload: $Character / $World"

Start-Process "steam://rungameid/892970"
