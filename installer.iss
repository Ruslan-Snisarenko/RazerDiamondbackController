; Inno Setup Script for Razer Diamondback Controller
#define MyAppName "Razer Diamondback Controller"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "@Carist"
#define MyAppURL "https://t.me/Carist"
#define MyAppExeName "RazerDiamondbackController.exe"

[Setup]
AppId={{D37E6B20-56B8-439D-8687-B9C0EAE31C42}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=C:\Users\rusua\Desktop\Projects\RazerDiamondbackController
OutputBaseFilename=RazerDiamondbackSetup
SetupIconFile=C:\Users\rusua\Desktop\Projects\RazerDiamondbackController\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Запускать автоматически в трее при старте Windows"; GroupDescription: "Автозапуск:"; Flags: unchecked

[Files]
Source: "C:\Users\rusua\Desktop\Projects\RazerDiamondbackController\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\rusua\Desktop\Projects\RazerDiamondbackController\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\rusua\Desktop\Projects\RazerDiamondbackController\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--tray"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
