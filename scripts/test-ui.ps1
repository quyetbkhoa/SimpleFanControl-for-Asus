param(
    [string]$OutputDirectory = 'docs\images\v2.3'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$buildOutput = Join-Path $root 'build\bin\x64\Release'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe = Join-Path $buildOutput 'UiSmokeTest.exe'

& (Join-Path $PSScriptRoot 'build.ps1')
& $compiler /nologo /target:exe "/out:$testExe" `
    "/win32manifest:$(Join-Path $root 'src\SimpleFanControlForAsus\app.manifest')" `
    "/r:$(Join-Path $buildOutput 'SimpleFanControlForAsus.exe')" `
    /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll `
    (Join-Path $root 'tools\UiSmokeTest.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI test compilation failed.' }

Copy-Item -LiteralPath (Join-Path $buildOutput 'SimpleFanControlForAsus.exe.config') `
    -Destination ($testExe + '.config')
$outputPath = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
} else {
    Join-Path $root $OutputDirectory
}
& $testExe $outputPath
if ($LASTEXITCODE -ne 0) { throw 'UI smoke test failed.' }
