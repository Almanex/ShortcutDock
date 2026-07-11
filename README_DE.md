[ English ](README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# ShortcutDock Benutzerdefinierte Shortcut-Leiste für Windows 11

> [!IMPORTANT]
> **Erste stabile Version verfügbar!** 
> Sie können die fertige kompilierte Datei **`ShortcutDock.exe`** von der Seite [Releases] (https://github.com/Almanex/ShortcutDock/releases/tag/v1.0.0) herunterladen und auf Ihrem Computer ausführen, ohne zusätzliche Bibliotheken zu installieren.

Schnellstart-Desktop-Dock mit Unterstützung für Glimmer-/Acryl-/Volltransparenzeffekte, automatischer vertikaler/horizontaler Ausrichtungsumschaltung, Drag-and-Drop-Unterstützung, Systemintegration (AppBar) und Taskleistenintegration.

Ausführliche Anweisungen zum Installieren, Starten und Konfigurieren aller Funktionen finden Sie im [Benutzerhandbuch (GUIDE.md)](GUIDE.md).

## Screenshots des Projekts

<p align="center">
 <img src="screenshots/screenshot1.png" width="48%" alt="ShortcutDock Horizontal Bottom" />
 <img src="screenshots/screenshot2.png" width="48%" alt="ShortcutDock Vertical Left" />
</p>
<p align="center">
 <img src="screenshots/screenshot3.png" width="48%" alt="ShortcutDock-Einstellungen" />
 <img src="screenshots/screenshot4.png" width="48%" alt="ShortcutDock-Kontextmenü" />
</p>
<p align="center">
 <img src="screenshots/screenshot5.png" width="98%" alt="ShortcutDock Top Position" />
</p>

## Technologie-Stack

| Komponente | Technologie | Version |
|-----------|-----------|--------|
| Язык | C# (.NET 8, LTS) | net8.0-windows |
| UI-фреймворк | WPF + **WPF-UI 4.3.0** (`FluentWindow` для настроек, Mica) | 4.3.0 |
| MVVM | **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`) | 8.4.2 |
| Win32 P/Invoke | Handbuch `DllImport` (user32, dwmapi, shell32) | |
| JSON | `System.Text.Json` (inline) | |
| Grafiken | `System.Drawing.Common` (Bitmap PNG für Icon-Cache, ICO-Upload) | 8.0.0 |

## Projektstruktur

```
D:\Develop\tsreen\
├── ShortcutDock.slnx              # Решение (новый XML-формат, SDK 10)
├── README.md                       # Этот файл
└── src\ShortcutDock\
    ├── ShortcutDock.csproj         # net8.0-windows, UseWPF, UseWindowsForms, UseSystemDrawing, ApplicationIcon
    ├── app.manifest                # PerMonitorV2 DPI, asInvoker, Win10/11 compat
    ├── App.xaml / App.xaml.cs      # Точка входа, DI (manual), инициализация системного трея (NotifyIcon)
    ├── MainWindow.xaml / .xaml.cs  # Главная панель, DWM-эффекты размытия, Alt+Tab hide, DnD, переключение ориентации
    ├── SettingsWindow.xaml / .cs   # Окно настроек (Mica, CardControl, переключатели)
    ├── app_icon.ico                # Встроенный значок приложения
    ├── Native\
    │   └── Win32.cs                # P/Invoke: GWL_EXSTYLE, DwmSetWindowAttribute, DwmExtendFrame,
    │                               #         RegisterWindowMessage, MonitorFromWindow
    ├── Models\
    │   ├── Settings.cs            
    │   ├── PanelSettings.cs        # Position, IconSize, KeepOnTop, BackdropType, ShowAddButton
    │   └── ShortcutItem.cs         # Id (GUID), Name, TargetPath, IconPath
    ├── Services\
    │   ├── SettingsService.cs      # Load/Save %AppData%\ShortcutDock\settings.json
    │   ├── ProcessLauncher.cs     # Process.Start(UseShellExecute=true, Verb="runas")
    │   ├── ShortcutResolver.cs     # .lnk → .exe через COM IShellLinkW + IPersistFile
    │   └── IconExtractor.cs       # SHGetImageList (JUMBO 256→EXTRALARGE 48→32) → PNG cache
    └── ViewModels\
        ├── MainViewModel.cs       # Коллекция ярлыков, AddViaDialog, AddFromFile, настройки, Persist
        └── ShortcutViewModel.cs   # Launch, RunAsAdmin, Remove команды
```

## Daten

Konfiguration: „%AppData%\ShortcutDock\settings.json“.

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

Symbol-Cache: „%AppData%\ShortcutDock\Cache\*.png“.

## Bedienelemente und Funktionen

### Hauptpanel
| Aktion | Wie |
|----------|-----|
| **Добавить ярлык** | Drag-and-Drop файла `.exe` / `.lnk` на панель, или кнопка **«+»** (если включена), или ПКМ по панели → «Добавить приложение» |
| **Anwendung starten/aktivieren** | Klicken Sie mit der linken Maustaste auf das Symbol. Wenn das Programm bereits ausgeführt wird (und die Anzeigen eingeschaltet sind), fokussiert der Klick und bringt das vorhandene Fenster in den Vordergrund, anstatt eine neue Kopie zu starten. |
| **Als Administrator ausführen** | RMB auf dem Symbol „Als Administrator ausführen“ |
| **Vom Bedienfeld entfernen** | RMB auf das Symbol „Aus Panel entfernen“ |
| **Einstellungen öffnen** | RMB auf dem freien Speicherplatz des Panel-Einstellungen-Panels oder des Taskleisten-Kontextmenüs |
| **Закрыть панель** | ПКМ по свободному месту панели → «Закрыть панель» или контекстное меню трея -> Выход |

### Taskleiste
- Im Windows-Infobereich (Taskleiste) wird ein benutzerdefiniertes Anwendungssymbol angezeigt.
- Doppelklicken Sie auf das Symbol, um das Dock-Panel zu verkleinern/erweitern.
- Über das Kontextmenü können Sie die Sichtbarkeit wechseln, das Einstellungsfenster öffnen oder die Anwendung beenden.

### Einstellungsfenster
- **Position auf dem Bildschirm:** Unten, Oben, Links, Rechts. Wenn Sie die linke oder rechte Seite auswählen, wechselt das Bedienfeld automatisch in die Hochformatausrichtung.
- **Unschärfeeffekt:** Keine (100 % Transparenz nur mit Symbolen), Glimmer, Acryl.
- **Symbolgröße:** 32 px, 40 px, 48 px, 64 px (dynamische Größenänderung).
- **Über allen Fenstern:** Aktiviert/deaktiviert das Andocken über anderen Fenstern und das Reservieren von Platz auf dem Desktop (AppBar). Wenn Sie ein Bedienfeld deaktivieren, wird es möglicherweise von anderen Fenstern überlagert.
- **Schaltfläche „+“ im Bedienfeld anzeigen:** Ermöglicht das Ausblenden der Schaltfläche „Hinzufügen“ im Dock für ein minimalistischeres Erscheinungsbild.
- **Bedienfeld automatisch ausblenden (Auto-Hide):** Das Bedienfeld wird sanft vom Bildschirm ausgeblendet, wenn Sie den Mausfokus verlieren (es bleibt ein 2-Pixel-Streifen zum Aufrufen übrig), wodurch die Reservierung des AppBar-Arbeitsbereichs vorübergehend aufgehoben wird, um Fenster von Drittanbietern vollständig zu maximieren.
- **Hover-Zoom-Effekt:** Zoomt Symbole beim Hover stufenlos im macOS-Stil und erzeugt so eine interaktive Welle.
- **Indikatoren für laufende Programme:** Zeigt Akzentfarbpunkte unter laufenden Programmen an und leitet den Klick um, um ein vorhandenes Fenster zu aktivieren.

---

## Änderungsverlauf

| Datum | Ändern |
|------|-----------|
| 2026-06-19 | Первичная реализация: проект, модели, сервисы, UI, AppBar |
| 2026-06-19 | Исправлены XAML-пространства имен WPF-UI и ссылки на `System.Drawing.Common` |
| 2026-06-19 | Исправлена работа Mica/Acrylic за счет перехода к композиции `AllowsTransparency="False"` + `WindowChrome` + DWM P/Invoke |
| 2026-06-19 | Исправлена логика AppBar (позиционирование, устранение бесконечного цикла изменения размеров в связке с `SizeToContent`) |
| 2026-06-19 | Разработана форма настроек Fluent-дизайна (SettingsWindow) с полной привязкой настроек в реальном времени |
| 2026-06-19 | Добавлена интеграция с системным треем (NotifyIcon, ContextMenuStrip) |
| 2026-06-19 | Реализована поддержка вертикальных ориентаций и адаптивных триггеров XAML |
| 2026-06-19 | Интегрирована собственная иконка `app_icon.ico` для сборки и трея. Очищены временные ресурсы. |
| 2026-06-24 | Устранена утечка и зависание AppBar при системных событиях `WM_SETTINGCHANGE`. Настроен автономный релиз. |
| 2026-06-28 | Исправлена работа Корзины: выравнивание структуры `SHQUERYRBINFO` на x64 системах, запуск папки кликом и сброс кэша значков на старте. |
| 2026-07-03 | Интегрирована синхронизация с системной темой (`SystemThemeWatcher`): фон панели (Mica/Acrylic) и элементы интерфейса теперь нативно меняют тему на темную/светлую. |
| 2026-07-03 | Добавлен премиум-функционал: автоскрытие (Auto-Hide), эффект увеличения значков (Hover Zoom), индикаторы запущенных приложений с восстановлением окон на передний план и анимации отскока (Bounce). Исправлен краш окна настроек. |