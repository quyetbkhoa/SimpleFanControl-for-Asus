# SimpleFanControl for Asus

Modern bilingual fan control for compatible Asus laptops. The project is based
on [Karmel0x/AsusFanControl](https://github.com/Karmel0x/AsusFanControl) and keeps
that repository as its upstream history.

![Dashboard](docs/images/v2.3/dashboard-vi.png)

## Features / Tính năng

- Separate English and Vietnamese interfaces, with a flag switch at the top right.
- Remembered language selection and DPI-aware text and charts.
- Live CPU temperature, fan RPM and applied output.
- Manual fan control and draggable temperature curve.
- Numeric point editor and keyboard alternatives to curve dragging.
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

The package is written to `dist\SimpleFanControl-for-Asus-v2.3.0` and includes
SHA-256 checksums.

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
