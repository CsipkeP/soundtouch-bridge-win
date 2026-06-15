; Bose SoundTouch Bridge — Inno Setup script
; Build:  iscc setup.iss   (vagy a build-installer.ps1 használata)

#define MyAppName "Bose SoundTouch Bridge"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Bose SoundTouch Bridge"
#define MyAppExeName "BoseSoundTouchBridge.exe"
#define MyAppId "{B05E705E-50F7-4F8C-B7E3-BC1E0F8F8B05}"

[Setup]
AppId={{#MyAppId}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\BoseSoundTouchBridge
DefaultGroupName={#MyAppName}
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
OutputDir=..\dist
OutputBaseFilename=BoseSoundTouchBridge-Setup-{#MyAppVersion}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile=..\Assets\app.ico
DisableProgramGroupPage=yes
ShowLanguageDialog=auto
CloseApplications=force
RestartApplications=no
MinVersion=10.0.17763

[Languages]
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "Indítás a Windows-szal"; GroupDescription: "Indítás:"

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "BoseSoundTouchBridge"; \
    ValueData: """{app}\{#MyAppExeName}"""; \
    Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Beállítások (settings.json) NEM törlődnek, %APPDATA%-ban maradnak.
; Ha a felhasználó újratelepít, megmaradnak a presetek és Spotify auth.
