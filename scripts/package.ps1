param(
    [string]$Version = '2.3.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$buildOutput = Join-Path $root 'build\bin\x64\Release'
$distRoot = Join-Path $root 'dist'
$package = Join-Path $distRoot "SimpleFanControl-for-Asus-v$Version"
$psExec = Join-Path $root 'tools\PsExec.exe'

& (Join-Path $PSScriptRoot 'build.ps1') -Configuration Release

if (-not (Test-Path -LiteralPath $psExec)) {
    throw 'tools\PsExec.exe is missing. Copy Microsoft Sysinternals PsExec.exe there before packaging.'
}

$distFull = [IO.Path]::GetFullPath($distRoot)
$packageFull = [IO.Path]::GetFullPath($package)
if (-not $packageFull.StartsWith($distFull + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Invalid package output path.'
}

if (Test-Path -LiteralPath $packageFull) {
    Remove-Item -LiteralPath $packageFull -Recurse -Force
}
New-Item -ItemType Directory -Path $packageFull -Force | Out-Null

$files = @(
    'AsusFanControl.exe',
    'AsusFanControl.exe.config',
    'SimpleFanControlForAsus.exe',
    'SimpleFanControlForAsus.exe.config',
    'AsusWinIO64.dll'
)

foreach ($file in $files) {
    Copy-Item -LiteralPath (Join-Path $buildOutput $file) -Destination $packageFull
}
Copy-Item -LiteralPath $psExec -Destination $packageFull
Copy-Item -LiteralPath (Join-Path $root 'docs\README-EN-VI.txt') -Destination $packageFull

$hashes = Get-ChildItem -LiteralPath $packageFull -File |
    Sort-Object Name |
    ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        '{0}  {1}' -f $hash.Hash, $_.Name
    }
$hashes | Set-Content -LiteralPath (Join-Path $packageFull 'SHA256SUMS.txt') -Encoding ASCII

Write-Host "Package created: $packageFull"
