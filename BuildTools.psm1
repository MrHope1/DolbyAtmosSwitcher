function Get-DotNetFrameworkToolchain {
    [CmdletBinding()]
    param(
        [string]$WindowsDirectory = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    )

    $frameworkFamilies = @("Framework64", "Framework")
    foreach ($family in $frameworkFamilies) {
        $frameworkDirectory = Join-Path $WindowsDirectory "Microsoft.NET\$family\v4.0.30319"
        $compilerPath = Join-Path $frameworkDirectory "csc.exe"
        $wpfLibraryPath = Join-Path $frameworkDirectory "WPF"

        if ((Test-Path -LiteralPath $compilerPath -PathType Leaf) -and
            (Test-Path -LiteralPath $wpfLibraryPath -PathType Container)) {
            return [pscustomobject]@{
                CompilerPath = $compilerPath
                WpfLibraryPath = $wpfLibraryPath
            }
        }
    }

    throw "A compatible .NET Framework 4.x C# compiler and WPF reference directory were not found under $WindowsDirectory."
}

Export-ModuleMember -Function Get-DotNetFrameworkToolchain
