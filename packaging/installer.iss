#ifndef AppVersion
  #define AppVersion "2.4.1"
#endif
#define AppName "SimpleFanControl for Asus"

[Setup]
AppId={{9F5796E2-8239-44A5-B80D-D752A176BFBB}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=quyetbkhoa
AppPublisherURL=https://github.com/quyetbkhoa/SimpleFanControl-for-Asus
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0
WizardStyle=modern
LicenseFile=..\LICENSE
SetupIconFile=..\src\SimpleFanControlForAsus\propeller.ico
UninstallDisplayIcon={app}\SimpleFanControlForAsus.exe
OutputDir=..\dist\installer
OutputBaseFilename=SimpleFanControl-for-Asus-v{#AppVersion}-Setup-x64
Compression=lzma2
SolidCompression=yes
CloseApplications=no
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "..\dist\SimpleFanControl-for-Asus-v{#AppVersion}\*"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\SimpleFanControlForAsus.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\SimpleFanControlForAsus.exe"; Tasks: desktopicon

[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  if Result then
    Result := Release >= 461808;
  if not Result then
    MsgBox('Install .NET Framework 4.7.2 or newer before installing this application.', mbError, MB_OK);
end;

function ApplicationRunning(): Boolean;
var
  Services, Processes: Variant;
begin
  Services := CreateOleObject('WbemScripting.SWbemLocator');
  Services := Services.ConnectServer('', 'root\CIMV2');
  Processes := Services.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name="SimpleFanControlForAsus.exe"');
  Result := Processes.Count > 0;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if ApplicationRunning() then
    Result := 'Exit SimpleFanControl for Asus from the system tray, then click Install again. This lets the application restore firmware fan control safely.';
end;

function InitializeUninstall(): Boolean;
begin
  Result := not ApplicationRunning();
  if not Result then
    MsgBox('Exit SimpleFanControl for Asus from the system tray before uninstalling.', mbError, MB_OK);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Scheduler, Folder, Task: Variant;
begin
  if CurUninstallStep = usUninstall then
  begin
    Scheduler := CreateOleObject('Schedule.Service');
    Scheduler.Connect();
    Folder := Scheduler.GetFolder('\');
    try
      Task := Folder.GetTask('SimpleFanControl for Asus');
    except
      Exit;
    end;
    if CompareText(Task.Definition.Actions.Item(1).Path, ExpandConstant('{app}\SimpleFanControlForAsus.exe')) = 0 then
      Folder.DeleteTask('SimpleFanControl for Asus', 0);
  end;
end;
