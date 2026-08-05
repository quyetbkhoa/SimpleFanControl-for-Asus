# SimpleFanControl for Asus

[![Build and release](https://github.com/quyetbkhoa/SimpleFanControl-for-Asus/actions/workflows/build-release.yml/badge.svg)](https://github.com/quyetbkhoa/SimpleFanControl-for-Asus/actions/workflows/build-release.yml)

Simple fan control for compatible ASUS laptops, with manual output and an
editable temperature curve.

## Download

Download the latest Windows ZIP from
[GitHub Releases](https://github.com/quyetbkhoa/SimpleFanControl-for-Asus/releases).
The repository itself contains source code; compiled executables are published
only as CI artifacts and tagged releases.

## Run

1. Extract the complete release ZIP.
2. Close an older version from the system tray.
3. Run `SimpleFanControlForAsus.exe` and accept the Windows UAC prompt.

<details>
    <summary>Command line: `AsusFanControl.exe`</summary>
    
    AsusFanControl.exe <args>
        --get-fan-speeds
        --set-fan-speeds=0-100 (percent value, 0 for turning off test mode)
        --get-fan-count
        --get-fan-speed=fanId (comma separated)
        --set-fan-speed=fanId:0-100 (comma separated, percent value, 0 for turning off test mode)
        --get-cpu-temp
</details>

GUI: `SimpleFanControlForAsus.exe`

![AsusFanControlGUI](https://github.com/Karmel0x/AsusFanControl/assets/25367564/fe197ad0-7079-4d51-ae78-177cb6369e96)

## Features

- Modern white and blue dashboard.
- Live CPU temperature, fan RPM and applied output.
- Manual fan output and an editable CPU temperature fan curve.
- Current CPU temperature marker on the curve.
- Adjustable polling: 1, 2, 3, 5 or 10 seconds (2 seconds by default).
- Automatic statistics refresh.
- Optional launch at Windows sign-in through a highest-privilege Scheduled Task.
- Minimize to system tray and restore firmware control on exit.

The curve can be edited at any time. Enable `Temperature curve`, then enable
`Fan control` to apply it. Settings are saved per Windows user. `Safe output
limits` restricts commands to 40–99%. The default curve is 30°C/40%,
55°C/50%, 70°C/65%, 80°C/90%, and 90°C/100%.

On systems that require the ASUS library to run as SYSTEM, keep `run.bat`
and `PsExec.exe` next to `SimpleFanControlForAsus.exe`. Double-clicking the
GUI executable redirects through that launcher.

## Repository structure

```text
.
|-- AsusFanControl/       # Command-line tool and ASUS driver wrapper
|-- AsusFanControlGUI/    # Windows Forms application
|-- packaging/            # Files included only when packaging a release
|-- .github/workflows/    # Build and release automation
`-- AsusFanControl.sln
```

Build requirements:

- Windows
- Visual Studio 2022 or MSBuild with the .NET Framework 4.7.2 targeting pack

Build from the command line:

```powershell
msbuild AsusFanControl.sln /m /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

The output is written to `bin\x64\Release`.

## Releases

Every push and pull request to `main` is built by GitHub Actions. A normal push
does **not** create a public release.

To publish an important version, create and push a version tag:

```powershell
git tag -a v2.1.0 -m "SimpleFanControl for Asus v2.1.0"
git push origin v2.1.0
```

GitHub Actions builds the source, creates one Windows x64 ZIP containing only
the required runtime files, and publishes that ZIP on the GitHub Releases page.
During packaging, CI downloads PsExec from the official Microsoft Sysinternals
archive and verifies its Microsoft signature instead of storing it in source.

## Why this project?
My laptop does not support the [Fan Profile](https://github.com/Karmel0x/AsusFanControl/assets/25367564/924d990a-bf20-4b8d-bf9d-56c460174d99) option, but it often overheats. Looked for apps to control fans, but none is working.

## Compatibility
This program should work on any laptop with x64 windows where [Fan Diagnosis](https://github.com/Karmel0x/AsusFanControl/assets/25367564/7129833b-97af-4da8-9148-b71e49552ea4) in [MyASUS](https://apps.microsoft.com/store/detail/myasus/9N7R5S6B0ZZH) application is working as it is using same library.

[ASUS System Control Interface](https://www.asus.com/support/faq/1047338/) is necessary for this software to work - `ASUS System Analysis` service [must be running](../../issues/16). It's automatically installed with `MyASUS` app.

Included `AsusWinIO64.dll` is licenced to `(c) ASUSTek COMPUTER INC.` which can be found in `C:\Windows\System32\DriverStore\FileRepository\asussci2.inf_amd64_-\ASUSSystemAnalysis\` if you have MyASUS installed.

[Works on](../../issues/13): 
- ASUS: VivoBook, ZenBook, TUF Gaming, ROG Strix, ROG Zephyrus, ROG Flow

## Credits and third-party components

This project is based on
[Karmel0x/AsusFanControl](https://github.com/Karmel0x/AsusFanControl).
`PsExec.exe` is part of
[Microsoft Sysinternals PsTools](https://learn.microsoft.com/sysinternals/downloads/psexec).
`AsusWinIO64.dll` belongs to ASUSTeK COMPUTER INC. See the project
[LICENSE](LICENSE) for the source-code license.
