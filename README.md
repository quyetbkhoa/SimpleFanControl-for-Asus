# SimpleFanControl for Asus

Modern bilingual fan control for compatible Asus laptops. The project is based
on [Karmel0x/AsusFanControl](https://github.com/Karmel0x/AsusFanControl) and keeps
that repository as its upstream history.

![Dashboard](docs/images/v2.4/dashboard-vi.png)

## Download / Tải về

Download the **Setup-x64.exe** from [GitHub Releases](https://github.com/quyetbkhoa/SimpleFanControl-for-Asus/releases/latest)
for a normal Windows installation, or **Portable-x64.zip** to run without
installation after extracting the entire folder. Both contain the stable PC
version 2.4.1, including its required ASUS DLL and PsExec launcher.

## Features / Tính năng

- Separate English and Vietnamese interfaces, with a flag switch at the top right.
- Remembered language selection and DPI-aware text and charts.
- Live CPU temperature, fan RPM and applied output.
- Manual fan control and draggable temperature curve.
- Three control states: Off (ASUS firmware), Manual (slider only), Curve (graph only).
- Restores the last selected control mode on launch, including Windows tray startup.
  Select your preferred mode once after upgrading from v2.4.0 or earlier.
- Add endpoints; dragging adjusts neighboring speeds to keep the curve monotonic.
- Keyboard and right-click step controls provide alternatives to dragging.
- Configurable rise/fall slew rates; upward smoothing is bypassed at observed CPU ≥90°C.
- Compact Windows-style navigation and DPI-scaled layouts.
- Current CPU temperature marker on the curve.
- Polling intervals of 1, 2, 3, 5 or 10 seconds; default is 2 seconds.
- Auto-refresh, system tray and automatic firmware reset on exit.
- Optional Windows sign-in startup through a highest-privilege Scheduled Task.

Default curve / Đường cong mặc định:

`30°C/40% → 55°C/50% → 70°C/65% → 80°C/90% → 90°C/100%`

## Repository layout / Cấu trúc

```text
src/
  AsusFanControl/             Core hardware and CLI
  SimpleFanControlForAsus/    WinForms desktop application
docs/                         Screenshots and user guide
scripts/                      Bootstrap, build and package scripts
tools/                        Local runtime dependency location
build/                        Generated binaries (ignored)
dist/                         Generated release packages (ignored)
archive/                      Recoverable legacy packages (ignored)
```

## Build / Biên dịch

Requirements:

- Windows x64
- .NET Framework 4.7.2 runtime
- PowerShell
- Visual Studio Build Tools, or the Windows Framework MSBuild fallback

```powershell
.\scripts\bootstrap.ps1
.\scripts\build.ps1
```

The build is written to `build\bin\x64\Release`.

To create a runnable package, place the official Microsoft Sysinternals
`PsExec.exe` at `tools\PsExec.exe`, then run:

```powershell
.\scripts\package.ps1
```

The package is written to `dist\SimpleFanControl-for-Asus-v2.4.1` and includes
SHA-256 checksums.

To create a single Windows installer EXE, install Inno Setup 6 and run:

```powershell
.\scripts\build-installer.ps1
```

The installer is written to `dist\installer`. Use `-UseExistingPackage` to wrap
the existing checksummed release without rebuilding the stable application.
The same command also creates a portable ZIP; extract the whole ZIP and open
`SimpleFanControlForAsus.exe`. The current .NET Framework application depends on
native ASUS libraries and PsExec, so its executable alone is not portable.
The Setup EXE is a single download that installs all of these dependencies.
Setup installs the complete package in Program Files, adds a Start menu shortcut
and an optional desktop shortcut, and registers Windows uninstall support.
Exit the application from the tray before installing or uninstalling. Uninstall
removes the startup task only if it points to this installation and keeps user
settings. Enable Start with Windows inside the installed app to migrate an
existing portable startup task. Setup requires Windows x64 and .NET Framework
4.7.2 or newer; ASUS services and drivers must already be installed.

## Run / Chạy ứng dụng

Double-click `SimpleFanControlForAsus.exe` from the complete generated package.
Windows may ask for administrator consent. The executable starts PsExec directly
to open the hardware interface as SYSTEM, without a CMD or batch launcher.
Keep `PsExec.exe` and the other release files beside the executable.
The optional Windows startup task also launches the EXE directly, in the system
tray, 15 seconds after sign-in. It runs on battery as well as AC power. Enable it
in Settings → Start with Windows. Previously enabled batch-based tasks are
migrated when the new application opens. Keep the release in a stable directory.

The ASUS System Control Interface and `ASUS System Analysis` service must be
installed and running. `AsusWinIO64.dll` is licensed to ASUSTek Computer Inc.

## Safety / An toàn

Fan control directly affects cooling hardware. Keep `Safe output limits`
enabled unless you understand the consequences. Turning control off or exiting
the application returns all fans to ASUS firmware control.

## UI verification

Run `scripts/test-ui.ps1` to exercise the real form without loading the ASUS driver.
This checks mouse language switching, chart dragging, mode selection, tray startup,
the Windows task definition (without registering it), and renders
both pages in Vietnamese and English. Preview values are simulated; this check
does not validate hardware control or the elevated PsExec launch.

The UI design rationale and the UI UX Pro Max searches are recorded in
[docs/UI_DESIGN.md](docs/UI_DESIGN.md).

The hardware backend still needs ASUS System Control Interface, its System
Analysis service and AsusWinIO64.dll. Removing the DLL requires a different,
model-validated hardware backend; bundling or renaming it would not remove that
dependency.
