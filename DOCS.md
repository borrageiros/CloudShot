# CloudShot



Quick reference for developers working on this codebase.



## Overview



CloudShot is a Windows tray application for screen capture with region selection, selection repositioning, annotations (pen, rectangle, filled rectangle, pixelate, arrow, highlighter, line, steps, text), color picker, OCR, and SCP upload. It runs in the background and is triggered via `PrintScreen` or the tray icon.



**Stack:** C# / .NET Framework 4.8, WinForms, `Microsoft.Windows.SDK.Contracts` (Windows OCR API).



**Build:** `dotnet build` (requires .NET SDK; targets `net48`).



**Settings file:** `%AppData%\CloudShot\settings.xml` (XML-serialized `AppSettings`).



---



## Application Flow



```

Program.Main

  → AppSettings.Load() (bootstrap only; MainForm loads settings again in its constructor)

  → MainForm (hidden, tray-only)

      → KeyboardHook registers PrintScreen

      → On capture: ScreenCaptureService.CaptureAllScreens()

      → ScreenshotOverlay (fullscreen form)

          → User selects region, annotates, exports

          → ScreenshotCaptured event → clipboard copy + tray notification

```



---



## Directory Map



| Path | Responsibility |

|------|----------------|

| `Program.cs` | Entry point, icon bootstrap, launches `MainForm` |

| `MainForm.cs` | Tray icon, hotkey hook, capture orchestration, `ScreenshotEventArgs` |

| `ScreenshotOverlay.cs` | **Main capture UI** — selection, move, drawing, color picker, OCR, SCP, keyboard/mouse handling |

| `AppSettings.cs` | Persistent settings (shortcuts, SCP, color format, default drawing color, startup, toolbar tool visibility) |

| `ConfigForm.cs` | Settings UI (shortcuts, SCP config, color format, default drawing color, startup toggle, toolbar tool toggles) |

| `KeyboardHook.cs` | Global hotkey via Win32 `RegisterHotKey` |

| `IconGenerator.cs` | Generates `app.ico` at runtime if missing |

| `Core/UpdateService.cs` | Checks GitHub latest release once on startup; compares with the assembly version |



### `Core/`



| File | Responsibility |

|------|----------------|

| `ScreenCaptureService.cs` | Multi-monitor bounds + `CopyFromScreen` capture |

| `CoordinateMapper.cs` | Screen ↔ client ↔ image coordinate conversion |

| `DrawingElement.cs` | Annotation model (`Points`, `DrawingToolMode`, `DrawingColor`, `StepNumber`, `Text`) |

| `BitmapPixelReader.cs` | Fast pixel read via `LockBits` (color picker) |

| `ScpUploadService.cs` | Runs the built-in `scp` (OpenSSH) to upload a file with SSH key auth; resolves host/user from `~/.ssh/config` |

| `SshConfigResolver.cs` | Maps host/IP to SSH config alias or `user@host` before running scp |

| `UpdateService.cs` | Queries `releases/latest` on GitHub, parses `tag_name`, and reports whether a newer version exists |



### `Overlay/`



| File | Responsibility |

|------|----------------|

| `OverlayRenderer.cs` | All overlay painting: dim layer, selection, handles, annotations, color picker UI |

| `CaptureToolbar.cs` | Floating toolbar (`CaptureToolbarAction` enum, dynamic sizing, tooltips, GDI+ line icons) |

| `CaptureShortcutHandler.cs` | Maps keyboard shortcuts → `CaptureShortcutAction` |

| `ColorFormatter.cs` | RGB / HEX / HSL string formatting |



### `Export/`



| File | Responsibility |

|------|----------------|

| `ImageExporter.cs` | Render selection + annotations to bitmap, save dialog, pixelation, annotation drawing helpers |



---



## Key Features → Where to Look



| Feature | Primary files | Notes |

|---------|---------------|-------|

| **Screen capture** | `Core/ScreenCaptureService.cs`, `MainForm.cs` | Captures all monitors into one bitmap |

| **Region selection** | `ScreenshotOverlay.cs`, `Core/CoordinateMapper.cs` | Drag to select; resize handles after selection |

| **Move selection** | `ScreenshotOverlay.cs`, `Overlay/CaptureToolbar.cs`, `Overlay/OverlayRenderer.cs` | Toolbar **Move** tool; drag inside selection to reposition with fixed width/height; annotations move with selection; no keyboard shortcut |

| **Pen / rectangle drawing** | `ScreenshotOverlay.cs`, `Core/DrawingElement.cs`, `Export/ImageExporter.cs` | `DrawingToolMode.Pen` / `Rectangle`; disabled while `isMoveMode` is active |

| **Filled rectangle** | `ScreenshotOverlay.cs`, `Core/DrawingElement.cs`, `Export/ImageExporter.cs` | Toolbar-only; drag like rectangle; fills with drawing color |

| **Pixelate** | `ScreenshotOverlay.cs`, `Core/DrawingElement.cs`, `Export/ImageExporter.cs` | Toolbar-only; drag like rectangle; pixelates underlying screenshot (`PixelateBlockSize = 10`) |

| **Arrow** | `ScreenshotOverlay.cs`, `Export/ImageExporter.cs` | Toolbar-only; two-point drag; shaft + filled arrowhead |

| **Highlighter** | `ScreenshotOverlay.cs`, `Export/ImageExporter.cs` | Toolbar-only; semi-transparent wide stroke (`HighlighterPenSize = 20`, `HighlighterAlpha = 96`) |

| **Line** | `ScreenshotOverlay.cs`, `Export/ImageExporter.cs` | Toolbar-only; straight line between two points |

| **Steps** | `ScreenshotOverlay.cs`, `Export/ImageExporter.cs` | Toolbar-only; click to place numbered circles; auto-increments `StepNumber` on `DrawingElement` |

| **Text** | `ScreenshotOverlay.cs`, `Core/DrawingElement.cs`, `Export/ImageExporter.cs` | Toolbar-only; click to open inline editor; `Enter` or blur commits; `Esc` cancels; stored in `DrawingElement.Text` |

| **Floating toolbar** | `Overlay/CaptureToolbar.cs` | Appears near selection; emits `ActionRequested`; size adapts to visible tools |

| **Toolbar tool visibility** | `AppSettings.cs`, `ConfigForm.cs`, `Overlay/CaptureToolbar.cs` | Per-tool `Tool*Enabled` booleans in Settings → General; `ConfigureVisibleTools()` filters buttons; SCP also requires non-empty `ScpHost` |

| **Keyboard shortcuts** | `Overlay/CaptureShortcutHandler.cs`, `AppSettings.cs` | Configurable; defaults in `ResetToDefaults()`; shortcuts work even when toolbar button is hidden |

| **Copy to clipboard** | `ScreenshotOverlay.cs`, `MainForm.cs` | `Ctrl+C` or toolbar; fires `ScreenshotCaptured` |

| **Save to file** | `Export/ImageExporter.cs` | `Ctrl+S`; PNG/JPEG via `SaveFileDialog` |

| **Undo** | `ScreenshotOverlay.cs` | `Ctrl+Z`; removes last `DrawingElement` |

| **Color picker (screen)** | `ScreenshotOverlay.cs`, `Overlay/OverlayRenderer.cs`, `Core/BitmapPixelReader.cs`, `Overlay/ColorFormatter.cs` | `Ctrl+V`; zoom preview; copies formatted color (RGB/HEX/HSL); no selection required |

| **Drawing color** | `ScreenshotOverlay.cs`, `Overlay/CaptureToolbar.cs` | Toolbar color button opens `ColorDialog` for annotation tools |

| **Default drawing color** | `AppSettings.cs` (`DefaultDrawingColor`), `ConfigForm.cs`, `ScreenshotOverlay.cs` | Configurable in Settings → General → Drawing; stored as hex; `ScreenshotOverlay` initializes `currentDrawingColor` from it (default `#FF0000`) |

| **OCR** | `ScreenshotOverlay.cs` (`PerformOcr`) | Uses `Windows.Media.Ocr.OcrEngine`; requires valid selection |

| **SCP upload** | `ScreenshotOverlay.cs` (`PerformScp`), `Core/ScpUploadService.cs`, `AppSettings.cs`, `ConfigForm.cs` | Structured config (host, port, remote path, SSH private key, optional key passphrase); uses the built-in OpenSSH `scp` command. Passphrase-protected keys use `SSH_ASKPASS`. Toolbar button hidden without host. |

| **Settings** | `AppSettings.cs`, `ConfigForm.cs` | Tray → Settings |

| **Start with Windows** | `MainForm.cs`, `ConfigForm.cs`, `AppSettings.cs` | Registry `HKCU\...\Run\CloudShot` |

| **Update check** | `Core/UpdateService.cs`, `MainForm.cs` (`CheckForUpdatesOnStartup`) | Runs once on `MainForm.Load`; shows a tray balloon if a newer GitHub release exists; clicking it opens the download page (`https://borrageiros.github.io/CloudShot/`) |



---



## Default Shortcuts



| Action | Default key |

|--------|-------------|

| Trigger capture | `PrintScreen` (global hotkey) |

| Copy | `Ctrl+C` |

| Save | `Ctrl+S` |

| Undo | `Ctrl+Z` |

| OCR | `Ctrl+R` |

| SCP upload | `Ctrl+X` |

| Color picker | `Ctrl+V` |

| Cancel / close overlay | `Esc` |



All overlay shortcuts are customizable in `ConfigForm` and stored in `AppSettings`. **Move**, **Filled rectangle**, **Pixelate**, **Arrow**, **Highlighter**, **Line**, **Steps**, and **Text** are toolbar-only (no default keyboard shortcut).



---



## Important Types & Events



```csharp

// Annotation modes

DrawingToolMode { Pen, Rectangle, FilledRectangle, Pixelate, Arrow, Highlighter, Line, Steps, Text }



// MainForm → overlay lifecycle

ScreenshotOverlay.ScreenshotCaptured → ScreenshotEventArgs { Image }



// Toolbar → overlay actions

CaptureToolbar.ActionRequested → CaptureToolbarAction (

  PenMode, RectangleMode, FilledRectangleMode, PixelateMode,

  ArrowMode, HighlighterMode, LineMode, StepsMode, TextMode,

  Move, ColorPicker, Undo, Copy, Save, Ocr, Scp, Close)



// Keyboard → overlay actions

CaptureShortcutHandler.TryHandle(...) → CaptureShortcutAction

```



---



## Conventions



- **Namespaces:** `CloudShot`, `CloudShot.Core`, `CloudShot.Overlay`, `CloudShot.Export`

- **UI framework:** WinForms only; overlay is a borderless fullscreen `Form` with `TopMost = true`

- **Coordinates:** Always go through `CoordinateMapper` when converting between screen, client, and image space

- **Rendering:** Live overlay uses `OverlayRenderer`; final export uses `ImageExporter.RenderSelection`

- **Comments:** prefer no comments in code; some legacy files still contain them

- **English** for all code identifiers, file names, and UI strings



---



## Common Edit Targets



| Task | Start here |

|------|------------|

| Change capture behavior | `ScreenshotOverlay.cs` |

| Change move-selection behavior | `ScreenshotOverlay.cs` (`MoveSelection`, `TranslateDrawingElements`) |

| Add toolbar button | `Overlay/CaptureToolbar.cs` + `AppSettings.cs` + `ConfigForm.cs` + handle in `ScreenshotOverlay.cs` |

| Change toolbar tool visibility defaults | `AppSettings.cs` (`ResetToolbarToolsToDefaults`) |

| Add keyboard shortcut | `AppSettings.cs` → `CaptureShortcutHandler.cs` → `ConfigForm.cs` |

| Change overlay visuals | `Overlay/OverlayRenderer.cs` |

| Change export output | `Export/ImageExporter.cs` |

| Change drawing tools / pixelate block size | `Export/ImageExporter.cs` (`PixelateBlockSize`), `Core/DrawingElement.cs` |

| Fix multi-monitor issues | `Core/ScreenCaptureService.cs`, `Core/CoordinateMapper.cs` |

| Change SCP behavior | `ScreenshotOverlay.cs` (`PerformScp`), `Core/ScpUploadService.cs` |

| Change OCR behavior | `ScreenshotOverlay.cs` (`PerformOcr`, `ExtractTextFromImageAsync`) |



---



## Build & Installer



- **Project file:** `CloudShot.csproj` (`net48`, `UseWindowsForms`)

- **Output:** `bin/Debug/net48/CloudShot.exe`

- **Installer:** `CloudShotSetup.iss` (Inno Setup; packages `bin/Debug/net48/*`)

- **User docs:** `README.md`



---



## Known Notes



- `ScreenshotOverlay.cs` is the largest file (~1500 lines) — most capture logic lives here

- Active drawing tool is tracked via `currentDrawingMode` (`DrawingToolMode`) in `ScreenshotOverlay` and `CaptureToolbar`

- Pixelate reads from the original screenshot and does not use the drawing color; block size is `ImageExporter.PixelateBlockSize` (default 10)

- `isColorSelected` field in `ScreenshotOverlay` is currently unused (CS0414 warning)

- `CaptureToolbar` requires `ControlStyles.SupportsTransparentBackColor` for `BackColor = Color.Transparent` (WinForms limitation)

- Toolbar icons are drawn in code with GDI+ (`DrawIcon`); there are no SVG/image assets for toolbar buttons

- `CaptureToolbar.ConfigureVisibleTools()` rebuilds the visible action list and recalculates toolbar size/orientation candidates

- SCP toolbar button requires both `ToolScpEnabled` and a non-empty `ScpHost`

- `AppSettings.SettingsVersion` migrates older settings files (v2 adds toolbar tool booleans, all enabled by default; v3 adds `ToolTextEnabled`)

- `Properties/Resources.resx` exists but is not used by capture logic

- Duplicate path separators exist in git for some `Overlay/` files (Windows path normalization) — functionally identical files

