$ErrorActionPreference = "Stop"

$tests = @(
    "Test-BuildToolchain.ps1",
    "Test-Core.ps1",
    "Test-BuildScript.ps1",
    "Test-InstallScript.ps1",
    "Test-CliExitCodes.ps1"
)

foreach ($test in $tests) {
    Write-Host "Running $test..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot $test)
}

Write-Host "All repository tests passed." -ForegroundColor Green
