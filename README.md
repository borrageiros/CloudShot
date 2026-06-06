# 📸 CloudShot

Lightweight screenshot tool for Windows. Capture a region, annotate it, copy or save it, extract text with OCR, or upload it via SCP.

## ⬇️ Download

Get the latest release from [download page](https://borrageiros.github.io/CloudShot/):

- **Installer** — `CloudShot-{version}-installer.exe`
- **Portable** — `CloudShot-{version}-portable.zip`

## ✨ Features

- Region capture across multiple monitors
- Move and resize the selection before exporting
- Pen and rectangle drawing tools
- Color picker with zoom preview (RGB, HEX, HSL)
- OCR with clipboard copy
- SCP upload using the built-in OpenSSH client
- Configurable keyboard shortcuts
- Optional start with Windows

## 🖱️ Usage

Press `PrintScreen` or use the tray icon to start a capture. Select an area, then copy, save, run OCR, or upload.

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

All overlay shortcuts can be changed in Settings. The Move tool is available from the toolbar only.

## ⚙️ Settings

Right-click the tray icon and open **Settings**.

- **General** — start with Windows
- **Shortcuts** — customize overlay keys
- **SCP** — upload configuration
- **Color picker** — output format (RGB, HEX, HSL)

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

During capture, press `Ctrl+V`, move the cursor to pick a pixel, and click to copy the color. Set the format in Settings.

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
