; ============================================================
;  Rx Writer — Inno Setup 6 installer script
;  Target: Windows 7 SP1+ (x86 and x64)
;  Build : dotnet publish -c Release -r win-x64 --self-contained true -o "publish\OPDClinic-x64"
;          dotnet publish -c Release -r win-x86 --self-contained true -o "publish\OPDClinic-x86"
; ============================================================

#define AppName        "Rx Writer"
#define AppVersion     "2.1.7"
#define AppPublisher   "Rx Writer"
#define AppExeName     "OPDClinic.exe"
#define SourceDirX64   "publish\OPDClinic-x64"
#define SourceDirX86   "publish\OPDClinic-x86"

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
; Support both 32-bit and 64-bit Windows (no ArchitecturesAllowed restriction)
; Install in 64-bit mode on 64-bit Windows
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired       =admin
UninstallDisplayIcon     ={app}\{#AppExeName}
UninstallDisplayName     ={#AppName}
VersionInfoVersion       ={#AppVersion}
VersionInfoCompany       ={#AppPublisher}
VersionInfoDescription   =Rx Writer Setup
LicenseFile              =
ShowLanguageDialog       =no
; Minimum: Windows 7 SP1 (6.1.7601)
MinVersion               =6.1.7601
; Auto-update support: close running instances before install, restart after
CloseApplications        =yes
RestartApplications      =yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
; 64-bit files — installed on 64-bit Windows
Source: "{#SourceDirX64}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsWin64
; 32-bit files — installed on 32-bit Windows
Source: "{#SourceDirX86}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not IsWin64

[Icons]
Name: "{group}\{#AppName}";  Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
// Nothing custom needed — standard install/uninstall flow.
