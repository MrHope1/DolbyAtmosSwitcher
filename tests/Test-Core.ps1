$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$programPath = Join-Path $repoRoot "Program.cs"
$source = Get-Content -LiteralPath $programPath -Raw
if ($source -match '\b(mouse_event|SimulateMouseClick|SetCursorPos|GetCursorPos|SendInput|MOUSEEVENTF_[A-Z]+)\b') {
    throw 'Program.cs must not use a native mouse-click fallback while Dolby Access is pinned off-screen.'
}

Import-Module (Join-Path $repoRoot "BuildTools.psm1") -Force
$toolchain = Get-DotNetFrameworkToolchain
$csc = $toolchain.CompilerPath
$libDir = $toolchain.WpfLibraryPath
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("DolbySwitcher-CoreTests-" + [Guid]::NewGuid().ToString("N"))
$testExe = Join-Path $tempDir "SafetyTests.exe"

New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $compilerArgs = @(
        "/nologo",
        "/target:exe",
        "/main:SafetyTests",
        "/define:TESTING",
        "/out:$testExe",
        "/lib:$libDir",
        "/r:System.dll",
        "/r:System.Drawing.dll",
        "/r:System.Windows.Forms.dll",
        "/r:UIAutomationClient.dll",
        "/r:UIAutomationTypes.dll",
        "/r:WindowsBase.dll",
        $programPath,
        (Join-Path $PSScriptRoot "SafetyTests.cs")
    )

    & $csc @compilerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Core safety test compilation failed with exit code $LASTEXITCODE."
    }

    & $testExe
    if ($LASTEXITCODE -ne 0) {
        throw "Core safety tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
