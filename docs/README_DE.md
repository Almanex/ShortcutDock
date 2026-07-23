# ShortcutDock

**Anpassbare Fluent-Design-Schnellstartleiste für Windows 11-Desktops**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue.svg)](#)
[![Framework: .NET 10](https://img.shields.io/badge/Framework-.NET%2010.0-blueviolet.svg)](https://dotnet.microsoft.com)
[![Share on X](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock)](https://twitter.com/intent/tweet?text=Schau%20dir%20ShortcutDock%20an%20--%20eine%20wundersch%C3%B6ne%2C%20anpassbare%20Schnellstartleiste%20f%C3%BCr%20Windows%2011%21&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock&hashtags=windows11,wpf,dotnet,opensource)

---

## Übersicht

ShortcutDock ist eine moderne, leichtgewichtige Windows-Desktop-Leiste zur Organisation Ihrer Verknüpfungen. Sie bietet Mica- und Acrylic-Weichzeichnungseffekte, die sich mit dem aktiven Windows-Systemdesign synchronisieren, Drag-and-Drop-Unterstützung, automatische Ausrichtung zwischen vertikaler und horizontaler Ausrichtung, Systemplatzreservierung (AppBar) und System-Tray-Integration.

> [!IMPORTANT]
> **Aktuelstes Release v2.1.0 verfügbar!**  
> Sie können die fertige, kompilierte Datei **`ShortcutDock.exe`** auf der [Releases](https://github.com/Almanex/ShortcutDock/releases/tag/v2.1.0)-Seite herunterladen und direkt auf Ihrem Computer ausführen, ohne zusätzliche Bibliotheken installieren zu müssen.

Ausführliche Anweisungen zur Konfiguration aller Funktionen finden Sie im [Benutzerhandbuch (GUIDE.md)](GUIDE_DE.md).

---

## Hauptfunktionen

- Desktop-Leiste: Ziehen Sie `.exe`-, `.lnk`-Dateien oder Ordner einfach per Drag-and-Drop direkt auf die Leiste, um sie hinzuzufügen.
- Ordner-Fächer (Folder Stacks): Popup-Fächeransicht für Ordnerverknüpfungen mit 1-Klick-Dateistart, Systemdesign-Synchronisation und anpassbarer Transparenz.
- Dateipfad öffnen: Schnelle Kontextmenüoption zum Anzeigen von Ziel-Dateien im Windows-Explorer.
- Dynamische Kapazitätsberechnung: Automatische Berechnung der maximalen Anzahl von Verknüpfungen basierend auf Bildschirmauflösung und DPI-Skalierung.
- Dynamische Ausrichtung: Wechselt automatisch zwischen vertikalen (linke/rechte Bildschirmseite) und horizontalen (oben/unten) Layouts.
- Modernes Design: Unterstützung für Mica- und Acrylic-Weichzeichnungseffekte, synchronisiert mit dem aktiven Windows-Systemdesign (hell/dunkel).
- Automatisches Ausblenden: Die Leiste wird sanft ausgeblendet, wenn der Mausfokus verloren geht, um die Arbeitsfläche zu maximieren.
- Hover-Zoom & Aktivitätsindikatoren: macOS-ähnliche Icon-Vergrößerungsanimation beim Überfahren mit der Maus und farbige Punkte unter geöffneten Anwendungen.

---

## Technologiestapel

| Ebene / Komponente | Technologie | Version | Zweck |
| --- | --- | --- | --- |
| Sprache | C# (.NET 10.0) | net10.0-windows | Hauptprogrammiersprache |
| UI-Framework | WPF + WPF-UI | 4.3.0 | Moderne Steuerelemente und Mica-Fensterrahmen |
| Entwurfsmuster | MVVM Toolkit | 8.4.2 | Zustandsbindung über CommunityToolkit.Mvvm |
| Win32-Integration | P/Invoke | - | APIs für DWM, Fensterstile und AppBar |
| Grafikbibliothek | System.Drawing.Common | 10.0.9 | Icon-Extraktion und PNG-Rendering |

---

## Erste Schritte

### Voraussetzungen
- .NET 10.0 SDK oder neuer

### Bauen & Ausführen
```powershell
# Repository klonen
git clone https://github.com/Almanex/ShortcutDock.git
cd ShortcutDock

# Abhängigkeiten wiederherstellen und bauen
dotnet build

# Projekt ausführen
dotnet run --project src\ShortcutDock
```

### Standalone Veröffentlichung
So kompilieren Sie eine einzelne, eigenständige ausführbare Datei:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Die fertige ausführbare Datei wird unter `src\ShortcutDock\bin\Release\net10.0-windows\win-x64\publish\` gespeichert.

---

## Tests ausführen
Dieses Projekt verwendet manuelle UI-Tests und automatisierte Build-Checks. So überprüfen Sie die Formatierung und den Build:
```powershell
dotnet build -c Release
```

---

## Mitwirken
Bitte senden Sie Fehlerberichte (Issues) und Pull Requests auf GitHub. Bei größeren Änderungen erstellen Sie bitte zuerst ein Issue zur Diskussion.

---

## Lizenz
Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe die `LICENSE`-Datei für Details.