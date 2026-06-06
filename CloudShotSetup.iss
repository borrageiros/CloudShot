#ifndef MyAppVersion
  #define MyAppVersion "2.5"
#endif

#ifndef MyOutputBaseFilename
  #define MyOutputBaseFilename "CloudShot_Setup"
#endif

[Setup]
AppName=CloudShot
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\CloudShot
DisableProgramGroupPage=yes
DisableStartupPrompt=yes
AlwaysShowDirOnReadyPage=yes
OutputBaseFilename={#MyOutputBaseFilename}
Compression=lzma
SolidCompression=yes
CreateUninstallRegKey=yes
; Uninstaller name
UninstallDisplayName=CloudShot
; Make uninstaller check if application is running
UninstallDisplayIcon={app}\CloudShot.exe
; Set default language to English
LanguageDetectionMethod=none
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\net48\CloudShot.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Icons]
Name: "{autodesktop}\CloudShot"; Filename: "{app}\CloudShot.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CloudShot.exe"; Description: "Launch CloudShot"; Flags: nowait postinstall skipifsilent
Filename: "{app}\CloudShot.exe"; Parameters: "--settings"; Description: "Open configuration"; Flags: nowait postinstall skipifsilent

[Code]
function IsAppRunning(const FileName: string): Boolean;
var
  FWMIService: Variant;
  FSWbemLocator: Variant;
  FWbemObjectSet: Variant;
  processName: string;
begin
  Result := False;
  processName := ExtractFileName(FileName);
  
  try
    FSWbemLocator := CreateOleObject('WbemScripting.SWbemLocator');
    FWMIService := FSWbemLocator.ConnectServer('', 'root\CIMV2', '', '');
    FWbemObjectSet := FWMIService.ExecQuery(Format('SELECT * FROM Win32_Process Where Name="%s"', [processName]));
    Result := FWbemObjectSet.Count > 0;
  except
    // If there's an error, assume the application is running to be safe
    Result := True;
  end;
end;

// Function to close CloudShot if it's running
function CloseCloudShot(): Boolean;
var
  Proc: Integer;
  exeName: String;
  resultCode: Integer;
begin
  Result := True;
  exeName := 'CloudShot.exe';
  
  // Check if CloudShot is running
  if IsAppRunning(exeName) then
  begin
    // Show message and ask for confirmation
    if MsgBox('CloudShot is currently running and needs to be closed before continuing.' + #13#10 +
              'Do you want to close it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Try to close the application with taskkill
      Exec('taskkill.exe', '/F /IM ' + exeName, '', SW_HIDE, ewWaitUntilTerminated, resultCode);
      
      // Wait a moment to make sure it closes
      Sleep(1000);
      
      // Check again if it's still running
      if IsAppRunning(exeName) then
      begin
        MsgBox('Could not close CloudShot. Please close it manually before continuing.', mbError, MB_OK);
        Result := False;
      end;
    end
    else
      Result := False;
  end;
end;

// Function that runs before uninstallation begins
function InitializeUninstall(): Boolean;
begin
  Result := CloseCloudShot();
end; 