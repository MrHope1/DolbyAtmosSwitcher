$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $repoRoot "BuildTools.psm1") -Force
$toolchain = Get-DotNetFrameworkToolchain
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("DolbySwitcher-CliTests-" + [Guid]::NewGuid().ToString("N"))
$exePath = Join-Path $tempDir "DolbyAtmosSwitcher.CliTests.exe"
$logPath = Join-Path $tempDir "debug_log.txt"
$variableName = "DOLBY_SWITCHER_TEST_LOG_DIRECTORY"
$previousOverride = [Environment]::GetEnvironmentVariable(
    $variableName,
    [EnvironmentVariableTarget]::Process)

New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $references = @(
        "System.dll",
        "System.Drawing.dll",
        "System.Windows.Forms.dll",
        "UIAutomationClient.dll",
        "UIAutomationTypes.dll",
        "WindowsBase.dll"
    )
    $compilerArgs = @(
        "/nologo",
        "/target:winexe",
        "/define:TESTING",
        "/out:$exePath",
        "/lib:$($toolchain.WpfLibraryPath)"
    ) + ($references | ForEach-Object { "/r:$_" }) + @((Join-Path $repoRoot "Program.cs"))

    & $toolchain.CompilerPath @compilerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "CLI test build failed with exit code $LASTEXITCODE."
    }

    [Environment]::SetEnvironmentVariable(
        $variableName,
        $tempDir,
        [EnvironmentVariableTarget]::Process)

    $sentinel = "PRIVATE_SENTINEL_CLI_TEST_91D6E4"
    $process = Start-Process -FilePath $exePath -ArgumentList "/change",$sentinel -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 2) {
        throw "Invalid CLI input returned exit code $($process.ExitCode), expected 2."
    }

    if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        throw "Expected isolated test log was not created."
    }
    if (Select-String -LiteralPath $logPath -SimpleMatch $sentinel -Quiet) {
        throw "Raw invalid CLI input leaked into the isolated test log."
    }

    Write-Host "PASS: invalid CLI input returns exit code 2 without logging raw arguments."
}
finally {
    [Environment]::SetEnvironmentVariable(
        $variableName,
        $previousOverride,
        [EnvironmentVariableTarget]::Process)
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
