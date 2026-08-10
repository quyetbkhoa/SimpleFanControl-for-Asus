# Script build Installer tu dong cho SimpleFanControl for Asus
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRootDir = Split-Path -Parent $scriptDir

Set-Location $projectRootDir

Write-Host "=== 1. Kiem tra va tim Visual Studio MSBuild ===" -ForegroundColor Cyan
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = $null

if (Test-Path $vswhere) {
    $msbuildDir = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    if ($msbuildDir -and (Test-Path $msbuildDir)) {
        $msbuild = $msbuildDir
    }
}

if (-not $msbuild) {
    $msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuildCmd) {
        $msbuild = $msbuildCmd.Source
    }
}

if (-not $msbuild) {
    Write-Error "Khong tim thay MSBuild.exe. Vui long cai dat Visual Studio hoac MSBuild Tools."
}

Write-Host "Dung MSBuild tai: $msbuild" -ForegroundColor Green

Write-Host "`n=== 2. Build solution ở che do Release x64 ===" -ForegroundColor Cyan
& $msbuild AsusFanControl.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build solution thất bại!"
}

Write-Host "`n=== 3. Kiem tra Inno Setup Compiler (ISCC) ===" -ForegroundColor Cyan
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $isccPath) {
        $iscc = $isccPath
    }
}

if (-not $iscc) {
    Write-Host "Chưa tìm thấy Inno Setup Compiler (iscc.exe)." -ForegroundColor Yellow
    Write-Host "Đang thử tự động cài đặt Inno Setup bằng winget..." -ForegroundColor Yellow
    winget install JRSoftware.InnoSetup --accept-source-agreements --accept-package-agreements
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $isccPath) {
        $iscc = $isccPath
    }
}

if (-not $iscc) {
    Write-Error "Khong tìm thấy Inno Setup Compiler. Vui lòng tải và cài đặt Inno Setup từ https://jrsoftware.org/isdl.php"
}

Write-Host "`n=== 4. Bien dich file Install.exe ===" -ForegroundColor Cyan
& $iscc packaging/installer.iss

$outputExe = "packaging\Output\SimpleFanControlForAsus_Setup.exe"
if (Test-Path $outputExe) {
    Write-Host "`n=== THANH CONG! ===" -ForegroundColor Green
    Write-Host "File cài đặt đã được tạo tại: $((Get-Item $outputExe).FullName)" -ForegroundColor Green
} else {
    Write-Error "Không tìm thấy file cài đặt sau khi biên dịch."
}
