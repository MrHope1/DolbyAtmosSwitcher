$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DolbySwitcher-BuildTests-" + [Guid]::NewGuid().ToString("N"))
$packageDir = Join-Path $tempRoot "package"
$attackerDir = Join-Path $tempRoot "attacker-working-directory"

New-Item -ItemType Directory -Path $packageDir | Out-Null
New-Item -ItemType Directory -Path $attackerDir | Out-Null

try {
    Copy-Item -LiteralPath (Join-Path $repoRoot "build.ps1") -Destination $packageDir
    Copy-Item -LiteralPath (Join-Path $repoRoot "BuildTools.psm1") -Destination $packageDir
    Copy-Item -LiteralPath (Join-Path $repoRoot "Program.cs") -Destination $packageDir

    Set-Content -LiteralPath (Join-Path $attackerDir "Program.cs") -Encoding UTF8 -Value @'
using System;
internal static class AttackerProgram
{
    private static void Main() { Console.WriteLine("wrong source"); }
}
'@

    Push-Location $attackerDir
    try {
        & (Join-Path $packageDir "build.ps1")
    }
    finally {
        Pop-Location
    }

    $packageOutput = Join-Path $packageDir "DolbyAtmosSwitcher.exe"
    $attackerOutput = Join-Path $attackerDir "DolbyAtmosSwitcher.exe"
    if (-not (Test-Path -LiteralPath $packageOutput -PathType Leaf)) {
        throw "Build output was not created beside build.ps1."
    }
    if (Test-Path -LiteralPath $attackerOutput -PathType Leaf) {
        throw "Build consumed or wrote to the ambient working directory."
    }

    $assemblyMetadata = [System.Text.Encoding]::UTF8.GetString(
        [System.IO.File]::ReadAllBytes($packageOutput))
    if (-not $assemblyMetadata.Contains("DolbyAtmosAppContext")) {
        throw "Build did not compile the package Program.cs."
    }
    if ($assemblyMetadata.Contains("AttackerProgram")) {
        throw "Build compiled Program.cs from the ambient working directory."
    }

    Write-Host "PASS: build.ps1 is anchored to its package source and output directory."
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
