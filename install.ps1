[CmdletBinding()]
param(
    [string]$TargetDir = (Join-Path $env:ProgramFiles "DolbySwicher"),
    [switch]$SkipProcessStop
)

$ErrorActionPreference = "Stop"
$sourceExe = Join-Path $PSScriptRoot "DolbyAtmosSwitcher.exe"

if (-not (Test-Path -LiteralPath $sourceExe -PathType Leaf)) {
    throw "DolbyAtmosSwitcher.exe was not found beside install.ps1. Run build.ps1 first."
}

$sourceItem = Get-Item -LiteralPath $sourceExe -Force
if (($sourceItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Refusing to install DolbyAtmosSwitcher.exe from a reparse point."
}

if (-not $SkipProcessStop) {
    Write-Host "Stopping any running switcher instances..." -ForegroundColor Cyan
    Stop-Process -Name DolbyAtmosSwitcher -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

Write-Host "Creating target folder at $TargetDir..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

Write-Host "Copying DolbyAtmosSwitcher.exe from the package directory..." -ForegroundColor Cyan
Copy-Item -LiteralPath $sourceExe -Destination (Join-Path $TargetDir "DolbyAtmosSwitcher.exe") -Force

Write-Host "Installation complete: $TargetDir" -ForegroundColor Green
