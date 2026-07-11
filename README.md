[ English ](README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# ShortcutDock

**Customizable Fluent design shortcut dock panel for Windows 11 desktops**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue.svg)](#)
[![Framework: .NET 8](https://img.shields.io/badge/Framework-.NET%208.0-blueviolet.svg)](https://dotnet.microsoft.com)
[![Share on X](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock)](https://twitter.com/intent/tweet?text=Check%20out%20ShortcutDock%20--%20a%20beautiful%20customizable%20shortcut%20dock%20for%20Windows%2011%21&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock&hashtags=windows11,wpf,dotnet,opensource)

---

## Overview

ShortcutDock is a modern, lightweight Windows desktop dock panel designed to organize your shortcuts. It features Mica and Acrylic blur effects that sync with the active Windows system theme, support for Drag-and-Drop, automatic vertical and horizontal layout orientation, system AppBar reservation, and tray integration.

> [!IMPORTANT]
> **First stable release available!**  
> You can download the ready-made compiled file **`ShortcutDock.exe`** on the [Releases](https://github.com/Almanex/ShortcutDock/releases/tag/v1.0.0) page and run it on your computer without installing additional libraries.

For detailed instructions on configuring all features, read the [User Guide (GUIDE.md)](GUIDE.md).

---

## Screenshots

<div style="display: flex; overflow-x: auto; scroll-snap-type: x mandatory; max-width: 100%; border: 2px solid #1e2327; border-radius: 8px; box-shadow: 4px 4px 0px #1e2327; margin-bottom: 20px;">
  <div style="flex: 0 0 100%; scroll-snap-align: start; padding: 15px; box-sizing: border-box; text-align: center;">
    <p style="font-weight: bold; margin-top: 0; font-family: sans-serif;">1. Horizontal Layout (Bottom)</p>
    <img src="screenshots/screenshot1.png" style="max-height: 380px; max-width: 100%; object-fit: contain;" alt="ShortcutDock Horizontal Bottom" />
  </div>
  <div style="flex: 0 0 100%; scroll-snap-align: start; padding: 15px; box-sizing: border-box; text-align: center;">
    <p style="font-weight: bold; margin-top: 0; font-family: sans-serif;">2. Vertical Layout (Left)</p>
    <img src="screenshots/screenshot2.png" style="max-height: 380px; max-width: 100%; object-fit: contain;" alt="ShortcutDock Vertical Left" />
  </div>
  <div style="flex: 0 0 100%; scroll-snap-align: start; padding: 15px; box-sizing: border-box; text-align: center;">
    <p style="font-weight: bold; margin-top: 0; font-family: sans-serif;">3. Fluent Settings Window</p>
    <img src="screenshots/screenshot3.png" style="max-height: 380px; max-width: 100%; object-fit: contain;" alt="ShortcutDock Settings" />
  </div>
  <div style="flex: 0 0 100%; scroll-snap-align: start; padding: 15px; box-sizing: border-box; text-align: center;">
    <p style="font-weight: bold; margin-top: 0; font-family: sans-serif;">4. Context Menu & Customizations</p>
    <img src="screenshots/screenshot4.png" style="max-height: 380px; max-width: 100%; object-fit: contain;" alt="ShortcutDock Context Menu" />
  </div>
  <div style="flex: 0 0 100%; scroll-snap-align: start; padding: 15px; box-sizing: border-box; text-align: center;">
    <p style="font-weight: bold; margin-top: 0; font-family: sans-serif;">5. Horizontal Layout (Top)</p>
    <img src="screenshots/screenshot5.png" style="max-height: 380px; max-width: 100%; object-fit: contain;" alt="ShortcutDock Top Position" />
  </div>
</div>


---

## Key Features

- Desktop Panel: Drag-and-drop `.exe` or `.lnk` files directly onto the panel to add.
- Dynamic Orientation: Switches between vertical (left/right screen positions) and horizontal (top/bottom) layouts.
- Modern Backdrop: Support for Mica and Acrylic blur effects in sync with the active Windows theme.
- Auto-Hide: Panel smoothly hides off-screen when mouse focus is lost to maximize workspace.
- Hover Zoom & Active Indicators: macOS-style icon zoom animation on hover and accent color dots under running applications.

---

## Tech Stack

| Layer / Component | Technology | Version | Purpose |
| --- | --- | --- | --- |
| Language | C# (.NET 8.0) | net8.0-windows | Main programming language |
| UI Framework | WPF + WPF-UI | 4.3.0 | Modern controls and Mica window shell |
| Pattern | MVVM Toolkit | 8.4.2 | CommunityToolkit.Mvvm for state binding |
| Win32 Interop | P/Invoke | - | DWM, WindowStyle, and AppBar APIs |
| Image Lib | System.Drawing.Common | 8.0.0 | Icon Extraction and PNG rendering |

---

## Project Structure

```text
ShortcutDock/
├── ShortcutDock.slnx              # Visual Studio Solution (SDK 10 format)
└── src\ShortcutDock\
    ├── ShortcutDock.csproj         # net8.0-windows configuration
    ├── app.manifest                # DPI awareness and compatibility manifest
    ├── App.xaml / App.xaml.cs      # Entry point, DI container, and Tray service
    ├── MainWindow.xaml / .xaml.cs  # Main panel, DWM blur, DnD, and orientation hooks
    ├── SettingsWindow.xaml / .cs   # Fluent settings window
    ├── app_icon.ico                # App icon asset
    ├── Native\
    │   └── Win32.cs                # P/Invoke helper definitions
    ├── Models\
    │   ├── Settings.cs            
    │   ├── PanelSettings.cs        # Panel preferences
    │   └── ShortcutItem.cs         # Shortcut model (GUID, path, cached icon)
    ├── Services\
    │   ├── SettingsService.cs      # Load/save settings.json in %AppData%
    │   ├── ProcessLauncher.cs     # Executes apps, supports admin elevation
    │   ├── ShortcutResolver.cs     # Resolves shell links (.lnk) via COM interfaces
    │   └── IconExtractor.cs       # Jumbo icon extraction (256x256) to PNG cache
    └── ViewModels\
        ├── MainViewModel.cs       # Handles main collection and settings
        └── ShortcutViewModel.cs   # Commands for launch, elevation, and deletion
```

---

## Data & Configuration

Configuration is saved in JSON format under `%AppData%\ShortcutDock\settings.json`:

```json
{
  "PanelSettings": {
    "Position": "Bottom",
    "IconSize": 48,
    "KeepOnTop": true,
    "BackdropType": "Mica",
    "ShowAddButton": false,
    "AutoHide": false,
    "HoverZoom": true,
    "ShowRunningIndicators": true
  },
  "Shortcuts": [
    {
      "Id": "a1b2c3d4-...",
      "Name": "Google Chrome",
      "TargetPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
      "IconPath": "%AppData%\\ShortcutDock\\Cache\\chrome_ABCD1234.png"
    }
  ]
}
```

Icon cache is stored under `%AppData%\ShortcutDock\Cache\*.png`.

---

## Getting Started

### Prerequisites
- .NET 8.0 SDK or newer

### Build & Run
```powershell
# Clone the repository
git clone https://github.com/Almanex/ShortcutDock.git
cd ShortcutDock

# Restore dependencies and build
dotnet build

# Run project
dotnet run --project src\ShortcutDock
```

### Standalone Publication
To compile a single executable with all dependencies bundled inside:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
The output file will be saved in `src\ShortcutDock\bin\Release\net8.0-windows\win-x64\publish\`.

---

## Running the Tests
This project uses manual UI verification and automated build checks. To verify code formatting:
```powershell
dotnet build -c Release
```

---

## Contributing
Please submit issues and pull requests on our GitHub repository. For major changes, open an issue first to discuss what you want to change.

---

## Versioning
We use SemVer for versioning. For available versions, see the tags on this repository.

---

## Authors & Acknowledgments
- Almanex - Developer and initial work.
- WPF-UI community for Fluent styling elements.

---

## License
This project is licensed under the MIT License - see the `LICENSE` file for details.