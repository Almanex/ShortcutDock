# ShortcutDock — Кастомная панель ярлыков для Windows 11

> [!IMPORTANT]
> **Доступен первый стабильный релиз!**  
> Вы можете скачать готовый скомпилированный файл **`ShortcutDock.exe`** на странице [Релизов (Releases)](https://github.com/Almanex/ShortcutDock/releases/tag/v1.0.0) и запустить его на своем компьютере без установки дополнительных библиотек.

Настольная dock-панель быстрого запуска с поддержкой эффектов Mica/Acrylic/полной прозрачности, автоматическим переключением вертикальной/горизонтальной ориентации, поддержкой Drag-and-Drop, системной интеграцией (AppBar) и интеграцией с системным треем.

Подробную инструкцию по установке, запуску и настройке всех функций читайте в [Руководстве пользователя (GUIDE.md)](GUIDE.md).

## Скриншоты проекта

<p align="center">
  <img src="screenshots/screenshot1.png" width="48%" alt="ShortcutDock Horizontal Bottom" />
  <img src="screenshots/screenshot2.png" width="48%" alt="ShortcutDock Vertical Left" />
</p>
<p align="center">
  <img src="screenshots/screenshot3.png" width="48%" alt="ShortcutDock Settings" />
  <img src="screenshots/screenshot4.png" width="48%" alt="ShortcutDock Context Menu" />
</p>
<p align="center">
  <img src="screenshots/screenshot5.png" width="98%" alt="ShortcutDock Top Position" />
</p>

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

## Данные

Конфигурация: `%AppData%\ShortcutDock\settings.json`

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

Кэш иконок: `%AppData%\ShortcutDock\Cache\*.png`

## Управление и функции

### Главная панель
| Действие | Как |
|----------|-----|
| **Добавить ярлык** | Drag-and-Drop файла `.exe` / `.lnk` на панель, или кнопка **«+»** (если включена), или ПКМ по панели → «Добавить приложение» |
| **Запустить/Активировать приложение** | Клик ЛКМ по иконке. Если программа уже запущена (и включены индикаторы), клик сфокусирует и выведет существующее окно на передний план вместо запуска новой копии. |
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
- **Автоматически скрывать панель (Auto-Hide):** Панель плавно скрывается за границы экрана при потере фокуса мыши (оставляя 2px полоску для вызова), временно снимая резервирование рабочего пространства AppBar для полного разворачивания сторонних окон.
- **Эффект увеличения при наведении (Hover Zoom):** Плавное масштабирование иконок при наведении курсора в стиле macOS, образующее интерактивную волну.
- **Индикаторы запущенных программ:** Отображение точек акцентного цвета под запущенными программами и перенаправление клика на активацию существующего окна.

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
| 2026-06-24 | Устранена утечка и зависание AppBar при системных событиях `WM_SETTINGCHANGE`. Настроен автономный релиз. |
| 2026-06-28 | Исправлена работа Корзины: выравнивание структуры `SHQUERYRBINFO` на x64 системах, запуск папки кликом и сброс кэша значков на старте. |
| 2026-07-03 | Интегрирована синхронизация с системной темой (`SystemThemeWatcher`): фон панели (Mica/Acrylic) и элементы интерфейса теперь нативно меняют тему на темную/светлую. |
| 2026-07-03 | Добавлен премиум-функционал: автоскрытие (Auto-Hide), эффект увеличения значков (Hover Zoom), индикаторы запущенных приложений с восстановлением окон на передний план и анимации отскока (Bounce). Исправлен краш окна настроек. |
