; Script Inno Setup cho SimpleFanControl for Asus
#define MyAppName "SimpleFanControl for Asus"
#define MyAppVersion "2.4.2"
#define MyAppPublisher "quyetbkhoa"
#define MyAppURL "https://github.com/quyetbkhoa/SimpleFanControl-for-Asus"
#define MyAppExeName "SimpleFanControlForAsus.exe"

[Setup]
AppId={{9F5796E2-8239-44A5-B80D-D752A176BFBB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=Output
OutputBaseFilename=SimpleFanControlForAsus_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\bin\x64\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\x64\Release\SimpleFanControlForAsus.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\x64\Release\AsusFanControl.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\x64\Release\AsusFanControl.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\..\bin\x64\Release\AsusFanControl.exe.config'))
Source: "..\AsusFanControl\AsusWinIO64.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "README-EN-VI.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninsexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
