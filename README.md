# 📸 CloudShot

Lightweight screenshot tool for Windows. Capture a region, annotate it, copy or save it, extract text with OCR, or upload it via SCP.

![CloudShot capture preview](https://raw.githubusercontent.com/borrageiros/CloudShot/refs/heads/main/screenshot.png)

## ⬇️ Download

Get the latest release from [download page](https://borrageiros.github.io/CloudShot/):

- **Installer** — `CloudShot-{version}-installer.exe`
- **Portable** — `CloudShot-{version}-portable.zip`

## ✨ Features

- Region capture across multiple monitors
- Move and resize the selection before exporting
- Drawing tools: pen, rectangle, filled rectangle, pixelate, arrow, highlighter, line, and numbered steps
- Configurable toolbar — show or hide each tool individually
- Color picker with zoom preview (RGB, HEX, HSL)
- OCR with clipboard copy
- SCP upload using the built-in OpenSSH client (toolbar button appears only when a host is configured)
- Configurable keyboard shortcuts
- Optional start with Windows

## 🖱️ Usage

Press `PrintScreen` or use the tray icon to start a capture. Select an area, then copy, save, run OCR, or upload.

### Drawing tools

After selecting a region, use the floating toolbar to annotate:

| Tool | Description |
|------|-------------|
| Pen | Freehand stroke in the chosen drawing color |
| Rectangle | Outline rectangle in the chosen drawing color |
| Filled rectangle | Solid rectangle filled with the chosen drawing color |
| Pixelate | Drag a rectangle to pixelate that area of the capture |
| Arrow | Drag from tail to tip to draw an arrow |
| Highlighter | Semi-transparent stroke to highlight an area |
| Line | Straight line in the chosen drawing color |
| Steps | Click to place numbered step markers (1, 2, 3, …) |
| Move | Reposition the selection without changing its size |

Set the drawing color with the **Color** button on the toolbar. **Move**, **Filled rectangle**, **Pixelate**, **Arrow**, **Highlighter**, **Line**, and **Steps** are toolbar-only (no keyboard shortcut).

### Default shortcuts

| Action | Key |
|--------|-----|
| Start capture | `PrintScreen` |
| Copy | `Ctrl+C` |
| Save | `Ctrl+S` |
| Undo | `Ctrl+Z` |
| OCR | `Ctrl+R` |
| Upload via SCP | `Ctrl+X` |
| Color picker | `Ctrl+V` |
| Cancel | `Esc` |

All overlay shortcuts can be changed in Settings. Toolbar-only tools have no default keyboard shortcut, but their actions still work via the toolbar when enabled.

## ⚙️ Settings

Right-click the tray icon and open **Settings**.

- **General** — start with Windows, color picker format, default drawing color, toolbar tool visibility
- **Shortcuts** — customize overlay keys
- **SCP** — upload configuration

### Toolbar tools

In **General → Toolbar tools**, enable or disable each toolbar button. The capture toolbar resizes automatically based on the visible tools. The **Upload** button is shown only when the tool is enabled **and** an SCP host is configured.

### SCP

Uploads use the system `scp` command (OpenSSH must be available in PATH).

| Field | Description |
|-------|-------------|
| Host | `user@server` or an SSH config alias |
| Port | SSH port (default 22) |
| Remote path | Destination folder on the server |
| SSH private key | Path to your key file |
| Key passphrase | Optional, for encrypted keys |
| Clipboard text | Optional text copied after upload; `<image>` is replaced with the filename |

Example clipboard text: `https://myserver.com/screenshots/<image>`

### Color picker

During capture, press `Ctrl+V`, move the cursor to pick a pixel, and click to copy the color. Set the format in **General → Color picker**.

### Drawing

Set the default annotation color in **General → Drawing**. It applies to pen, rectangle, filled rectangle, arrow, highlighter, line, and steps.

## 📋 Requirements

- Windows
- .NET Framework 4.8 (included in the installer/portable build)
- OpenSSH client (for SCP upload)

## 🏗️ Build

```bash
dotnet restore
dotnet build
```

Requires the [.NET SDK](https://dotnet.microsoft.com/download) targeting .NET Framework 4.8.
