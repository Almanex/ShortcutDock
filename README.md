# ShortcutDock — Кастомная панель ярлыков для Windows 11

Настольная dock-панель быстрого запуска с поддержкой эффектов Mica/Acrylic/полной прозрачности, автоматическим переключением вертикальной/горизонтальной ориентации, поддержкой Drag-and-Drop, системной интеграцией (AppBar) и интеграцией с системным треем.

## Стек технологий

| Компонент | Технология | Версия |
|-----------|-----------|--------|
| Язык | C# (.NET 8, LTS) | net8.0-windows |
| UI-фреймворк | WPF + **WPF-UI 4.3.0** (`FluentWindow` для настроек, Mica) | 4.3.0 |
| MVVM | **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`) | 8.4.2 |
| Win32 P/Invoke | Ручные `DllImport` (user32, dwmapi, shell32) | — |
| JSON | `System.Text.Json` (встроенный) | — |
| Графика | `System.Drawing.Common` (Bitmap → PNG для кэша иконок, загрузка ICO) | 8.0.0 |

## Структура проекта

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
    │   ├── Settings.cs            # Корень JSON: PanelSettings + Shortcuts[]
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

## Данные

Конфигурация: `%AppData%\ShortcutDock\settings.json`

```json
{
  "PanelSettings": {
    "Position": "Bottom",
    "IconSize": 48,
    "KeepOnTop": true,
    "BackdropType": "Mica",
    "ShowAddButton": false
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

Кэш иконок: `%AppData%\ShortcutDock\Cache\*.png`

## Управление и функции

### Главная панель
| Действие | Как |
|----------|-----|
| **Добавить ярлык** | Drag-and-Drop файла `.exe` / `.lnk` на панель, или кнопка **«+»** (если включена), или ПКМ по панели → «Добавить приложение» |
| **Запустить приложение** | Клик ЛКМ по иконке |
| **Запустить от имени администратора** | ПКМ по иконке → «Запустить от имени администратора» |
| **Удалить с панели** | ПКМ по иконке → «Удалить с панели» |
| **Открыть настройки** | ПКМ по свободному месту панели → «Настройки панели» или контекстное меню трея |
| **Закрыть панель** | ПКМ по свободному месту панели → «Закрыть панель» или контекстное меню трея -> Выход |

### Системный трей
- В области уведомлений Windows (трее) отображается кастомная иконка приложения.
- Двойной клик по иконке сворачивает / разворачивает док-панель.
- Контекстное меню позволяет переключить видимость, открыть окно настроек или выйти из приложения.

### Окно настроек
- **Положение на экране:** Снизу, Сверху, Слева, Справа. При выборе левой или правой стороны панель автоматически переключается на вертикальную ориентацию.
- **Эффект размытия:** Нет (100% прозрачность с отображением одних лишь значков), Mica, Acrylic.
- **Размер значков:** 32 px, 40 px, 48 px, 64 px (динамически изменяет размеры).
- **Поверх всех окон:** Включает/выключает закрепление поверх других окон и резервирование места на рабочем столе (AppBar). При отключении панели другие окна могут перекрывать её.
- **Показывать кнопку «+» на панели:** Позволяет скрыть кнопку добавления на док-панели для более минималистичного вида.

---

## История изменений

| Дата | Изменение |
|------|-----------|
| 2026-06-19 | Первичная реализация: проект, модели, сервисы, UI, AppBar |
| 2026-06-19 | Исправлены XAML-пространства имен WPF-UI и ссылки на `System.Drawing.Common` |
| 2026-06-19 | Исправлена работа Mica/Acrylic за счет перехода к композиции `AllowsTransparency="False"` + `WindowChrome` + DWM P/Invoke |
| 2026-06-19 | Исправлена логика AppBar (позиционирование, устранение бесконечного цикла изменения размеров в связке с `SizeToContent`) |
| 2026-06-19 | Разработана форма настроек Fluent-дизайна (SettingsWindow) с полной привязкой настроек в реальном времени |
| 2026-06-19 | Добавлена интеграция с системным треем (NotifyIcon, ContextMenuStrip) |
| 2026-06-19 | Реализована поддержка вертикальных ориентаций и адаптивных триггеров XAML |
| 2026-06-19 | Интегрирована собственная иконка `app_icon.ico` для сборки и трея. Очищены временные ресурсы. |
