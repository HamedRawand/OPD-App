; ============================================================
;  Rx Writer — Inno Setup 6 installer script
;  Target: Windows 10/11 x64
;  Build : dotnet publish -c Release -r win-x64 --self-contained true
; ============================================================

#define AppName        "Rx Writer"
#define AppVersion "2.0.6"
#define AppPublisher   "Rx Writer"
#define AppExeName     "OPDClinic.exe"
#define SourceDir      "publish\OPDClinic"

[Setup]
AppId                    ={{A3F2C8D1-4B7E-4F5A-9D2E-1C6B8A3F0E5D}
AppName                  ={#AppName}
AppVersion               ={#AppVersion}
AppPublisher             ={#AppPublisher}
AppPublisherURL          =
AppSupportURL            =
AppUpdatesURL            =
DefaultDirName           ={autopf}\{#AppName}
DefaultGroupName         ={#AppName}
DisableProgramGroupPage  =yes
OutputDir                =installer_output
OutputBaseFilename       =OPDClinic_Setup_v{#AppVersion}
SetupIconFile            =app.ico
Compression              =lzma2/ultra64
SolidCompression         =yes
WizardStyle              =modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed     =x64compatible
PrivilegesRequired       =admin
UninstallDisplayIcon     ={app}\{#AppExeName}
UninstallDisplayName     ={#AppName}
VersionInfoVersion       ={#AppVersion}
VersionInfoCompany       ={#AppPublisher}
VersionInfoDescription   =Rx Writer Setup
LicenseFile              =
; Show a friendly finish page
ShowLanguageDialog       =no
; Require Windows 10 or later
MinVersion               =10.0
; Auto-update support: close running instances before install, restart after
CloseApplications        =yes
RestartApplications      =yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
; Copy everything from the publish output — recursive, preserving subdirectories
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start-menu entry
Name: "{group}\{#AppName}";  Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
; Desktop shortcut (only when the task above is selected)
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon
; Uninstall shortcut in Start menu (optional, modern apps often skip this)
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Run]
; Optionally launch the app after install
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall

[UninstallDelete]
; Remove any log files the app writes next to itself
Type: filesandordirs; Name: "{app}\logs"

[Code]
// Nothing custom needed — standard install/uninstall flow.
