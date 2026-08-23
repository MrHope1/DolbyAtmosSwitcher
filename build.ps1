$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "BuildTools.psm1") -Force
$toolchain = Get-DotNetFrameworkToolchain
$csc = $toolchain.CompilerPath
$libDir = $toolchain.WpfLibraryPath

# Assemblies to link
$references = @(
    "System.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "UIAutomationClient.dll",
    "UIAutomationTypes.dll",
    "WindowsBase.dll"
)

Write-Host "Compiling Program.cs..." -ForegroundColor Cyan
$sourcePath = Join-Path $PSScriptRoot "Program.cs"
$outputPath = Join-Path $PSScriptRoot "DolbyAtmosSwitcher.exe"
$compilerArgs = @(
    "/nologo",
    "/target:winexe",
    "/out:$outputPath",
    "/lib:$libDir"
) + ($references | ForEach-Object { "/r:$_" }) + @($sourcePath)

& $csc @compilerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with compiler exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

if (Test-Path -LiteralPath $outputPath -PathType Leaf) {
    Write-Host "Build Succeeded: DolbyAtmosSwitcher.exe generated." -ForegroundColor Green
} else {
    Write-Error "Build Failed!"
    exit 1
}
