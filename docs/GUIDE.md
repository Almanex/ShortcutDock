# ShortcutDock User Guide — Custom Dock Panel for Windows 10 & 11

> [!NOTE]
> **ShortcutDock** is a modern and functional dock panel for Windows 11 and 10 that allows you to easily launch applications, open folders, and manage system components with smooth effects and Mica/Acrylic styles. This guide explains how to install, configure, and customize the dock.

This guide will help you quickly understand all the features of the application and configure it to suit your needs.

---

## Table of Contents
1. [System Requirements](#1-system-requirements)
2. [Quick-Start Instructions](#quick-start-instructions)
3. [Adding Shortcuts to the Panel](#3-adding-shortcuts-to-the-panel)
4. [Managing and Sorting Shortcuts](#4-managing-and-sorting-shortcuts)
5. [Configuring Appearance and Effects](#5-configuring-appearance-and-effects)
6. [Recycle Bin Integration](#6-recycle-bin-integration)
7. [System Tray Integration](#7-system-tray-integration)
8. [Where Settings are Stored (Portability)](#8-where-settings-are-stored-portability)
9. [Troubleshooting](#9-troubleshooting)

---

## 1. System Requirements
* **Operating System:** Windows 10 (build 19041 and higher) or Windows 11.
* **Architecture:** x64.
* **Optional:** The Lightweight version requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or higher) installed. The Self-Contained version runs without any additional runtimes.

---

## Quick-Start Instructions

1. **Step 1: Download the Application** — Go to the releases page on GitHub and download the latest `ShortcutDock.exe` file:
   * [ShortcutDock Releases on GitHub](https://github.com/Almanex/ShortcutDock/releases)
2. **Step 2: Place the Executable** — Move the downloaded `ShortcutDock.exe` file to any convenient folder on your computer (e.g., `C:\Program Files\ShortcutDock` or your personal user directory).
3. **Step 3: Launch the Dock** — Double-click the `ShortcutDock.exe` file to run it. On the first launch, an empty semi-transparent panel with a `+` button will appear at the bottom of the screen.
   > [!IMPORTANT]
   > **Windows Defender SmartScreen Warning:**  
   > Since the application executable is not signed with a paid commercial developer digital certificate (which is normal for free open-source projects), Windows Defender SmartScreen might block the launch on the first run, showing a *"Windows protected your PC"* popup.  
   > **How to run the application:** Click the **"More info"** link (in the top-left area of the warning popup), and then click the appeared **"Run anyway"** button. Windows will remember your choice, and the warning won't appear on subsequent launches.

---

## 3. Adding Shortcuts to the Panel
You can add new programs, folders, or drives to the panel in three main ways:

### Method A: Drag-and-Drop
* **Files, Folders & Drives:** Simply drag any executable file (`.exe`), shortcut (`.lnk`), folder, or a drive from Windows File Explorer and drop it onto any empty space on the panel.
* **Start Menu Dragging:** You can drag application shortcuts directly from the Windows Start Menu (including UWP and Microsoft Store apps and games). ShortcutDock will automatically resolve their App User Model ID (AUMID) and create a functioning shortcut.

### Method B: Via the "+" Button
1. Click the `+` icon on the panel (if enabled in settings).
2. In the file dialog, select a program file or shortcut and click "Open".

### Method C: Via the Panel Context Menu
* Right-click on any empty space of the panel. You have two options:
  * **"Add File or Program..."** — opens a standard file selection dialog.
  * **"Open All Installed Apps Folder (Microsoft Store & Win32)..."** — opens the system virtual folder `shell:AppsFolder` containing all installed applications (both desktop apps and Microsoft Store apps). You can drag icons directly from this folder onto ShortcutDock.

---

## 4. Managing and Sorting Shortcuts

### Reordering Icons
* Hold the left mouse button on any icon and drag it left/right (or up/down for a vertical panel) to change its position relative to other items. Release the mouse button at the desired position — the order is saved automatically.

### Run as Administrator
* Right-click (RMB) the desired application on the panel and select **"Run as Administrator"**.

### Open File Location
* Right-click the shortcut on the panel and select **"Open File Location"** to open the target folder in Windows Explorer with the file selected.

### Changing the Icon
1. Right-click the shortcut and select **"Change Icon..."**.
2. Select any `.png` image or `.ico` icon file. The panel will instantly update the icon.

### Removing from the Panel
* Right-click the shortcut on the panel and select **"Remove from Panel"** (the program file on the disk will not be affected).

### Folder Stacks / Fan View
* Adding a folder to the dock (e.g. *Downloads* or *Documents*) opens a stylish popup fan view showing files inside when clicked. You can launch any file directly from the fan or click **"Open in Explorer"**.

---

## 5. Configuring Appearance and Effects
To open the settings window, right-click on any empty space on the panel or on the application tray icon and select **"Panel Settings"**.

* **Screen Position:** 
  * *Bottom* or *Top* (horizontal panel).
  * *Left* or *Right* (the panel automatically switches to vertical mode).
  * When moved to screen edges, the panel automatically reserves desktop space (AppBar technology) — other windows will not overlap it when maximized.
* **Blur Effect:**
  * *None:* Fully transparent background (only icons float over the wallpaper).
  * *Mica:* Windows 11 signature semi-transparent effect matching your wallpaper color. Automatically switches between dark and light themes depending on system settings.
  * *Acrylic:* Translucent frosted glass effect. Also dynamically adapts to system theme changes (light/dark).
* **Icon Size:**
  * You can select icon sizes: **32px**, **40px**, **48px**, or **64px**. The panel resizes instantly.
  * To prevent the dock from exceeding screen boundaries, maximum item limits apply (including Recycle Bin):
    * **32px:** up to 21 items
    * **40px:** up to 17 items
    * **48px:** up to 15 items
    * **64px:** up to 12 items
* **Keep on Top:**
  * When enabled, the panel is always visible on top of other windows and reserves desktop space. When disabled, the panel behaves like a normal window and can be overlapped.
* **Interface Language:**
  * Allows you to switch the application language (*English*, *Русский*, *Deutsch*). The changes apply instantly to all elements, including context menus and system tray. On first launch, the language is chosen automatically based on Windows settings.
* **Show "+" button on the panel:**
  * Allows you to hide the add button on the dock panel for a clean, minimalist look (enabled by default on first launch).
* **Start with Windows:**
  * Adds the application to Windows startup folder to launch the panel automatically when you log in.
* **Auto-Hide:**
  * When enabled, the panel smoothly slides out of the screen when it loses mouse focus, leaving a thin 2px trigger strip. Hovering the mouse over this strip instantly brings the panel back. The AppBar space reservation is temporarily disabled while the panel is hidden, allowing other windows to occupy the full screen. The panel will not hide if a context menu is open.
* **Hover Zoom Effect:**
  * Enables a smooth wave-like icon scaling effect on mouse hover, similar to macOS dock. This makes the selected shortcut stand out and easier to click.
* **Show Indicators for running applications:**
  * Displays small accent-colored dots under icons of currently active programs.
  * Clicking an active application brings its existing window to focus instead of launching a new instance.
* **Folder fan background transparency:**
  * Convenient slider for smooth adjustment of folder fan popup transparency (30% to 100%).

---

## 6. Recycle Bin Integration
In the panel settings, you can enable the option **"Show Recycle Bin on the panel"**.

* **Dynamic State:** The Recycle Bin icon changes in real-time depending on whether it is empty or contains deleted files.
* **Empty Recycle Bin:** Right-click the Recycle Bin icon on the panel and select **"Empty Recycle Bin"**.
* **Open Recycle Bin:** Left-click the Recycle Bin icon to open it in Windows File Explorer.
* *Note:* The Recycle Bin is always pinned to the very end of the panel; it cannot be dragged to the middle.

---

## 7. System Tray Integration
When ShortcutDock is running, its icon appears in the system tray (near the Windows clock).

* **Quick Show/Hide:** Double-click the tray icon to quickly hide the panel (or restore it).
* **Tray Context Menu:** Right-click the tray icon to open Settings or exit the application completely ("Exit").

---

## 8. Where Settings are Stored (Portability)
All user settings, added shortcuts, and cache icons are stored in your user profile directory:
`%AppData%\ShortcutDock\` (typically `C:\Users\Username\AppData\Roaming\ShortcutDock`).

* **Configuration File:** `settings.json` — contains settings for positions, sizes, and the list of shortcuts.
* **Cache Folder:** `Cache\` — stores extracted PNG icons for fast rendering on startup.
* All paths within the configuration file are saved in a portable format (using the `%AppData%` variable), allowing you to copy settings to another computer easily.

---

## 9. Troubleshooting

### Application icons display incorrectly
* Try removing the shortcut and adding the application again, or manually replace the icon using the context menu -> "Change Icon...".

### The panel overlaps the Windows Taskbar
* Change the panel position in settings. For example, if the Windows taskbar is at the bottom, pin ShortcutDock to the top, left, or right edge to avoid interface overlap.

### Autostart does not work
* Make sure you haven't moved the `ShortcutDock.exe` file after enabling autostart. If you did, simply disable and re-enable autostart in the settings to update the path in the Windows registry.

---

## Join the Community & Support

- **Star the Repository**: If you find this project helpful, please give us a star on [GitHub](https://github.com/Almanex/ShortcutDock)!
- **Report Bugs & Ideas**: Open an issue if you encounter any problems or have feature requests.
- **Contribute**: Feel free to submit pull requests to help improve the project.
