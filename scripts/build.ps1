param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'SimpleFanControlForAsus.sln'
$compilerDirectory = Join-Path $root '.tools\roslyn\tasks\net472'
$compiler = Join-Path $compilerDirectory 'csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    & (Join-Path $PSScriptRoot 'bootstrap.ps1')
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path -LiteralPath $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
        Select-Object -First 1
}

if (-not $msbuild) {
    $msbuild = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
}

if (-not (Test-Path -LiteralPath $msbuild)) {
    throw 'MSBuild was not found. Install Visual Studio Build Tools or .NET Framework build tools.'
}

& $msbuild $solution `
    /t:Rebuild `
    /p:Configuration=$Configuration `
    /p:Platform=x64 `
    /p:CscToolPath=$compilerDirectory `
    /p:CscToolExe=csc.exe `
    /m `
    /verbosity:minimal

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

Write-Host "Build completed: build\bin\x64\$Configuration"
