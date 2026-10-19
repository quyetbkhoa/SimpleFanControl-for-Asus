param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$toolRoot = Join-Path $root '.tools\roslyn'
$packagePath = Join-Path $root '.tools\cache\Microsoft.Net.Compilers.Toolset.4.8.0.nupkg'
$compilerPath = Join-Path $toolRoot 'tasks\net472\csc.exe'

if (Test-Path -LiteralPath $compilerPath) {
    Write-Host "Roslyn compiler is already available: $compilerPath"
    exit 0
}

New-Item -ItemType Directory -Path (Split-Path -Parent $packagePath) -Force | Out-Null
Write-Host 'Downloading Microsoft.Net.Compilers.Toolset 4.8.0 from NuGet...'
Invoke-WebRequest `
    -Uri 'https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers.Toolset/4.8.0' `
    -OutFile $packagePath

New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
Expand-Archive -LiteralPath $packagePath -DestinationPath $toolRoot -Force
Write-Host "Compiler installed: $compilerPath"
