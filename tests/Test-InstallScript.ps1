$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot "install.ps1"
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors)

if ($parseErrors.Count -ne 0) {
    throw "install.ps1 has PowerShell parse errors."
}

$parameterNames = @($ast.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
if ($parameterNames -notcontains "TargetDir" -or $parameterNames -notcontains "SkipProcessStop") {
    throw "install.ps1 does not expose safe TargetDir and SkipProcessStop parameters for isolated verification."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DolbySwitcher-InstallTests-" + [Guid]::NewGuid().ToString("N"))
$packageDir = Join-Path $tempRoot "package"
$attackerDir = Join-Path $tempRoot "attacker-working-directory"
$targetDir = Join-Path $tempRoot "installed"

New-Item -ItemType Directory -Path $packageDir | Out-Null
New-Item -ItemType Directory -Path $attackerDir | Out-Null

try {
    Copy-Item -LiteralPath $scriptPath -Destination $packageDir
    Set-Content -LiteralPath (Join-Path $packageDir "DolbyAtmosSwitcher.exe") -Encoding ASCII -NoNewline -Value "trusted-package-binary"
    Set-Content -LiteralPath (Join-Path $attackerDir "DolbyAtmosSwitcher.exe") -Encoding ASCII -NoNewline -Value "attacker-working-directory-binary"
    Set-Content -LiteralPath (Join-Path $attackerDir "debug_log.txt") -Encoding ASCII -NoNewline -Value "private runtime log"

    Push-Location $attackerDir
    try {
        & (Join-Path $packageDir "install.ps1") -TargetDir $targetDir -SkipProcessStop
    }
    finally {
        Pop-Location
    }

    $installedExe = Join-Path $targetDir "DolbyAtmosSwitcher.exe"
    if (-not (Test-Path -LiteralPath $installedExe -PathType Leaf)) {
        throw "Installer did not copy the expected executable."
    }
    if ((Get-Content -Raw -LiteralPath $installedExe) -ne "trusted-package-binary") {
        throw "Installer copied the executable from the ambient working directory."
    }
    if (Test-Path -LiteralPath (Join-Path $targetDir "debug_log.txt")) {
        throw "Installer copied a private runtime log."
    }

    Write-Host "PASS: install.ps1 copies only the package executable and excludes runtime logs."
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
