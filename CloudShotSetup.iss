[Setup]
AppName=CloudShot
AppVersion=1.0
DefaultDirName={autopf}\CloudShot
DisableProgramGroupPage=yes
DisableStartupPrompt=yes
AlwaysShowDirOnReadyPage=yes
OutputBaseFilename=CloudShot_Setup
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
Source: "bin\Debug\net48\CloudShot.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Debug\net48\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Icons]
Name: "{autodesktop}\CloudShot"; Filename: "{app}\CloudShot.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CloudShot.exe"; Description: "Launch CloudShot"; Flags: nowait postinstall skipifsilent

[Code]
var
  ScpCommandPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  // Create page for SCP configuration
  ScpCommandPage := CreateInputQueryPage(wpSelectDir, 
    'SCP Configuration', 
    'Configure SCP parameters (optional)',
    'You can configure these values later from the application.');
  
  ScpCommandPage.Add('SCP Command (use <image> as reference to the file):', False);
  ScpCommandPage.Add('Text to copy (optional, use <image> as reference):', False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsPath: String;
  SettingsFile: String;
  ScpCommand, ScpClipboard: String;
begin
  if CurStep = ssPostInstall then
  begin
    ScpCommand := ScpCommandPage.Values[0];
    ScpClipboard := ScpCommandPage.Values[1];
    
    if (ScpCommand <> '') or (ScpClipboard <> '') then
    begin
      // Create settings folder
      SettingsPath := ExpandConstant('{localappdata}\CloudShot');
      if not DirExists(SettingsPath) then
        CreateDir(SettingsPath);
      
      // Create basic settings file
      SettingsFile := SettingsPath + '\settings.xml';
      SaveStringToFile(SettingsFile,
        '<?xml version="1.0"?>' + #13#10 +
        '<AppSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" ' +
        'xmlns:xsd="http://www.w3.org/2001/XMLSchema">' + #13#10 +
        '  <UndoShortcut>524346</UndoShortcut>' + #13#10 +
        '  <SaveShortcut>524355</SaveShortcut>' + #13#10 +
        '  <CopyShortcut>524339</CopyShortcut>' + #13#10 +
        '  <CancelShortcut>27</CancelShortcut>' + #13#10 +
        '  <OcrShortcut>524338</OcrShortcut>' + #13#10 +
        '  <ScpShortcut>524344</ScpShortcut>' + #13#10 +
        '  <ColorPickerShortcut>524342</ColorPickerShortcut>' + #13#10 +
        '  <StartWithWindows>False</StartWithWindows>' + #13#10 +
        '  <ScpCommand>' + ScpCommand + '</ScpCommand>' + #13#10 +
        '  <ScpClipboardText>' + ScpClipboard + '</ScpClipboardText>' + #13#10 +
        '  <ColorFormat>RGB</ColorFormat>' + #13#10 +
        '</AppSettings>', False);
    end;
  end;
end;

// Function to check if CloudShot is running
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