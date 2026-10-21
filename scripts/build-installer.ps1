param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '2.4.1',
    [string]$IsccPath,
    [switch]$UseExistingPackage
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $UseExistingPackage) {
    & (Join-Path $PSScriptRoot 'package.ps1') -Version $Version
}

$package = Join-Path $root "dist\SimpleFanControl-for-Asus-v$Version"
foreach ($line in Get-Content -LiteralPath (Join-Path $package 'SHA256SUMS.txt')) {
    if ($line -notmatch '^([A-Fa-f0-9]{64})  ([^\\/]+)$') {
        throw 'Invalid package checksum entry.'
    }
    $expected = $Matches[1]
    $file = Join-Path $package $Matches[2]
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $expected) {
        throw "Package checksum mismatch: $file"
    }
}

if (-not $IsccPath) {
    $IsccPath = @(
        (Join-Path $root '.tools\innosetup\ISCC.exe'),
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $IsccPath) {
    throw 'Install Inno Setup 6 or specify -IsccPath.'
}
& $IsccPath "/DAppVersion=$Version" (Join-Path $root 'packaging\installer.iss')
if ($LASTEXITCODE -ne 0) {
    throw "Installer compilation failed: $LASTEXITCODE"
}
$setup = Join-Path $root "dist\installer\SimpleFanControl-for-Asus-v$Version-Setup-x64.exe"
$hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
"$hash  $([IO.Path]::GetFileName($setup))" | Set-Content -LiteralPath "$setup.sha256" -Encoding ASCII
Write-Host "Installer created: $setup"
$portable = Join-Path $root "dist\installer\SimpleFanControl-for-Asus-v$Version-Portable-x64.zip"
$portableFiles = @((Get-ChildItem -LiteralPath $package -File).FullName) + @(Join-Path $root 'LICENSE')
Compress-Archive -LiteralPath $portableFiles -DestinationPath $portable -CompressionLevel Optimal -Force
$portableHash = (Get-FileHash -LiteralPath $portable -Algorithm SHA256).Hash
"$portableHash  $([IO.Path]::GetFileName($portable))" | Set-Content -LiteralPath "$portable.sha256" -Encoding ASCII
Write-Host "Portable ZIP created: $portable"
