# 📸 CloudShot

CloudShot is a modern, powerful, and lightweight screenshot application for Windows that combines advanced features like OCR and automatic uploading via SCP.

## ✨ Main Features

### 📷 Screenshot Capture
- 🎯 Precisely select any area of the screen
- 🖌️ Drawing and annotation tools
- 🔄 Undo function for corrections

### 🎨 Color Picker
- 🔍 Zoom in to select exact pixels
- 📋 Copy color values in multiple formats
- 🌈 Support for RGB, HEX, and HSL formats

### 🔍 Text Recognition (OCR)
- 📝 Extracts text from any image or screenshot
- 🌍 Supports multiple languages
- 📋 Direct copy to clipboard

### ⚡ Automatic Upload via SCP
- 🚀 Secure transfer to remote servers
- ⚙️ Customizable configuration
- 🔗 Automatic URL copying

### ⌨️ Keyboard Shortcuts
- `PrintScreen`: Start capture
- `Ctrl+C`: Copy to clipboard
- `Ctrl+S`: Save as file
- `Ctrl+Z`: Undo last action
- `Ctrl+R`: Extract text (OCR)
- `Ctrl+X`: Upload via SCP
- `Ctrl+V`: Activate color picker
- `Esc`: Cancel

## 🛠️ Installation

1. Download the installer from the releases section
2. Run the installer
3. Select the installation folder
4. Ready to use!

## ⚙️ General Configuration
Access the settings from the system tray icon:
1. 🖱️ Right-click on the CloudShot icon
2. ⚙️ Select "Settings"
3. 🔧 Adjust:
   - Keyboard shortcuts
   - Color picker format
   - SCP commands
   - Auto-start with Windows

## 🎨 Color Picker Configuration
CloudShot's color picker lets you extract colors from anywhere on your screen:

1. **Activation**:
   - Press `Ctrl+V` (customizable) during screen capture
   - Move the cursor to select a pixel color
   - Click to copy the color value to clipboard

2. **Output Format**:
   - Configure the format in Settings: RGB, HEX, or HSL
   - Example outputs:
     - RGB: `rgb(255, 0, 0)`
     - HEX: `#FF0000`
     - HSL: `hsl(0, 100%, 50%)`

## 📤 SCP Configuration
CloudShot allows configuring two important parameters for SCP functionality:

1. **SCP Command** (`SCP Command:`):
   - Defines the command to upload files to your server
   - Example: `scp <image> user@server:/path/to/upload/`
   - `<image>` will be replaced with the file to be uploaded

2. **URL to Copy** (`URL to copy:`):
   - Defines the base URL that will be copied to the clipboard after uploading
   - Example: `https://myserver.com/screenshots/<image>`
   - `<image>` will be replaced with the uploaded file name


## 🏗️ Compilation

### Requirements
- Visual Studio 2019 or later
- .NET Framework 4.8

### Compilation Steps
```bash
dotnet restore
dotnet build
