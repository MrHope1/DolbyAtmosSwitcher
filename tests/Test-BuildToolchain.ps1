$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $repoRoot "BuildTools.psm1") -Force

$actual = Get-DotNetFrameworkToolchain
if (-not (Test-Path -LiteralPath $actual.CompilerPath -PathType Leaf)) {
    throw "Discovered C# compiler does not exist."
}
if (-not (Test-Path -LiteralPath $actual.WpfLibraryPath -PathType Container)) {
    throw "Discovered WPF library directory does not exist."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DolbySwitcher-ToolchainTests-" + [Guid]::NewGuid().ToString("N"))
$frameworkDir = Join-Path $tempRoot "Microsoft.NET\Framework\v4.0.30319"
$wpfDir = Join-Path $frameworkDir "WPF"
$compilerPath = Join-Path $frameworkDir "csc.exe"

New-Item -ItemType Directory -Path $wpfDir -Force | Out-Null
Set-Content -LiteralPath $compilerPath -Encoding ASCII -NoNewline -Value "test compiler"

try {
    $fallback = Get-DotNetFrameworkToolchain -WindowsDirectory $tempRoot
    if ($fallback.CompilerPath -ne $compilerPath) {
        throw "Toolchain discovery did not fall back to the Framework directory."
    }
    if ($fallback.WpfLibraryPath -ne $wpfDir) {
        throw "Toolchain discovery returned the wrong WPF directory."
    }

    Write-Host "PASS: .NET Framework toolchain discovery is Windows-directory aware and supports Framework fallback."
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
