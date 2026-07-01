[Setup]
AppName=BanglaHost
AppVersion=1.3.3
DefaultDirName={autopf}\BanglaHost
DefaultGroupName=BanglaHost
OutputDir=Output
OutputBaseFilename=BanglaHost_Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=src\BanglaHost.App\Assets\AppIcon.ico

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "src\BanglaHost.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\BanglaHost"; Filename: "{app}\BanglaHost.App.exe"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{commondesktop}\BanglaHost"; Filename: "{app}\BanglaHost.App.exe"; Tasks: desktopicon; IconFilename: "{app}\Assets\AppIcon.ico"

[Run]
Filename: "{app}\BanglaHost.App.exe"; Description: "{cm:LaunchProgram,BanglaHost}"; Flags: nowait postinstall skipifsilent
