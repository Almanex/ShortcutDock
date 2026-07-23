# ShortcutDock

**Customizable Fluent design shortcut dock panel for Windows 11 desktops**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue.svg)](#)
[![Framework: .NET 10](https://img.shields.io/badge/Framework-.NET%2010.0-blueviolet.svg)](https://dotnet.microsoft.com)
[![Share on X](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock)](https://twitter.com/intent/tweet?text=Check%20out%20ShortcutDock%20--%20a%20beautiful%20customizable%20shortcut%20dock%20for%20Windows%2011%21&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock&hashtags=windows11,wpf,dotnet,opensource)

---

## Overview

ShortcutDock is a modern, lightweight Windows desktop dock panel designed to organize your shortcuts. It features Mica and Acrylic blur effects that sync with the active Windows system theme, support for Drag-and-Drop, automatic vertical and horizontal layout orientation, system AppBar reservation, and tray integration.

> [!IMPORTANT]
> **Latest release v2.1.1 available!**  
> You can download the ready-made compiled file **`ShortcutDock.exe`** on the [Releases](https://github.com/Almanex/ShortcutDock/releases/tag/v2.1.1) page and run it on your computer without installing additional libraries.

For detailed instructions on configuring all features, read the [User Guide (GUIDE.md)](docs/GUIDE.md).

---

## Key Features

- Desktop Panel: Drag-and-drop `.exe`, `.lnk`, folders, or Microsoft Store (UWP) apps directly onto the panel to add shortcuts.
- Folder Stacks (Fan View): Popup grid preview for folder shortcuts with 1-click file launching, theme synchronization, and adjustable background transparency.
- Open File Location: Quick context menu option to reveal target executable files in Windows Explorer.
- Dynamic Monitor-Aware Capacity: Automatically calculates maximum allowed dock items based on active screen resolution and DPI scaling.
- Dynamic Orientation: Switches between vertical (left/right screen positions) and horizontal (top/bottom) layouts.
- Modern Backdrop: Support for Mica and Acrylic blur effects in sync with the active Windows theme.
- Auto-Hide: Panel smoothly hides off-screen when mouse focus is lost to maximize workspace.
- Hover Zoom & Active Indicators: macOS-style icon zoom animation on hover and accent color dots under running applications.

---

## Tech Stack

| Layer / Component | Technology | Version | Purpose |
| --- | --- | --- | --- |
| Language | C# (.NET 10.0) | net10.0-windows | Main programming language |
| UI Framework | WPF + WPF-UI | 4.3.0 | Modern controls and Mica window shell |
| Pattern | MVVM Toolkit | 8.4.2 | CommunityToolkit.Mvvm for state binding |
| Win32 Interop | P/Invoke | - | DWM, WindowStyle, and AppBar APIs |
| Image Lib | System.Drawing.Common | 10.0.9 | Icon Extraction and PNG rendering |

---

## Getting Started

### Prerequisites
- .NET 10.0 SDK or newer

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
The output file will be saved in `src\ShortcutDock\bin\Release\net10.0-windows\win-x64\publish\`.

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

## License
This project is licensed under the MIT License - see the `LICENSE` file for details.
