[ English ](README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# ShortcutDock

**Настраиваемая панель быстрого запуска в стиле Fluent Design для рабочего стола Windows 11**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue.svg)](#)
[![Framework: .NET 10](https://img.shields.io/badge/Framework-.NET%2010.0-blueviolet.svg)](https://dotnet.microsoft.com)
[![Share on X](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock)](https://twitter.com/intent/tweet?text=%D0%9F%D0%BE%D1%81%D0%BC%D0%BE%D1%82%D1%80%D0%B8%D1%82%D0%B5%20%D0%BD%D0%B0%20ShortcutDock%20--%20%D0%BA%D1%80%D0%B0%D1%81%D0%B8%D0%B2%D1%83%D1%8E%20%D0%BF%D0%B0%D0%BD%D0%B5%D0%BB%D1%8C%20%D0%B1%D1%8B%D1%81%D1%82%D1%80%D0%BE%D0%B3%D0%BE%20%D0%B7%D0%B0%D0%BF%D1%83%D1%81%D0%BA%D0%B0%20%D0%B4%D0%BB%D1%8F%20Windows%2011%21&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FShortcutDock&hashtags=windows11,wpf,dotnet,opensource)

---

## Обзор

ShortcutDock — это современная, легковесная панель ярлыков для рабочего стола Windows, предназначенная для удобной организации ваших приложений. Она поддерживает эффекты размытия Mica и Acrylic, синхронизацию с системной темой Windows, технологию Drag-and-Drop, автоматическое переключение между вертикальной и горизонтальной ориентациями, резервирование экранного пространства (AppBar) и интеграцию с системным треем.

> [!IMPORTANT]
> **Доступен первый стабильный релиз!**  
> Вы можете скачать готовый скомпилированный файл **`ShortcutDock.exe`** на странице [Релизы](https://github.com/Almanex/ShortcutDock/releases/tag/v1.0.0) и запустить его на своем компьютере без установки дополнительных библиотек.

Подробные инструкции по настройке всех функций приведены в [Руководстве пользователя (GUIDE.md)](GUIDE.md).

---

## Скриншоты

<details open>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 1. Горизонтальное расположение (Снизу)</b></summary>
  <br/>
  <p align="center">
    <img src="screenshots/screenshot1.png" width="95%" alt="ShortcutDock Горизонтальная внизу" />
  </p>
</details>

<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 2. Вертикальное расположение (Слева)</b></summary>
  <br/>
  <p align="center">
    <img src="screenshots/screenshot2.png" width="95%" alt="ShortcutDock Вертикальная слева" />
  </p>
</details>

<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 3. Окно настроек Fluent Design</b></summary>
  <br/>
  <p align="center">
    <img src="screenshots/screenshot3.png" width="95%" alt="ShortcutDock Настройки" />
  </p>
</details>

<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 4. Контекстное меню и настройки</b></summary>
  <br/>
  <p align="center">
    <img src="screenshots/screenshot4.png" width="95%" alt="ShortcutDock Контекстное меню" />
  </p>
</details>

<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 5. Горизонтальное расположение (Сверху)</b></summary>
  <br/>
  <p align="center">
    <img src="screenshots/screenshot5.png" width="95%" alt="ShortcutDock Позиция сверху" />
  </p>
</details>






---

## Основные возможности

- Панель на рабочем столе: Перетаскивайте файлы `.exe` или `.lnk` с помощью Drag-and-Drop прямо на панель для их быстрого добавления.
- Динамическая ориентация: Автоматически переключается между вертикальным (при размещении слева или справа на экране) и горизонтальным (сверху или снизу) режимами отображения.
- Современное размытие: Поддержка эффектов размытия Mica и Acrylic с динамической сменой системной темы оформления (темная/светлая).
- Автоскрытие: Панель плавно скрывается за пределы экрана при потере фокуса мыши, освобождая рабочую область.
- Эффект Hover Zoom и индикаторы запуска: macOS-анимация увеличения значков при наведении и аккуратные точки акцентного цвета под запущенными программами.

---

## Стек технологий

| Слой / Компонент | Технология | Версия | Назначение |
| --- | --- | --- | --- |
| Язык | C# (.NET 10.0) | net10.0-windows | Основной язык разработки |
| UI-фреймворк | WPF + WPF-UI | 4.3.0 | Современные элементы управления и окно Mica |
| Паттерн | MVVM Toolkit | 8.4.2 | Связывание состояния через CommunityToolkit.Mvvm |
| Интеграция с Win32 | P/Invoke | - | Работа с DWM, стилями окон и API AppBar |
| Графика | System.Drawing.Common | 10.0.9 | Извлечение значков и рендеринг в PNG |

---

## Структура проекта

```text
ShortcutDock/
├── ShortcutDock.slnx              # Файл решения Visual Studio (формат SDK 10)
└── src\ShortcutDock\
    ├── ShortcutDock.csproj         # Конфигурация проекта net10.0-windows
    ├── app.manifest                # Манифест совместимости и поддержки DPI
    ├── App.xaml / App.xaml.cs      # Точка входа, DI-контейнер и служба трея
    ├── MainWindow.xaml / .xaml.cs  # Главная панель, обработка размытия DWM, DnD и AppBar
    ├── SettingsWindow.xaml / .cs   # Окно настроек Fluent-дизайна
    ├── app_icon.ico                # Встроенный значок приложения
    ├── Native\
    │   └── Win32.cs                # Системные вызовы P/Invoke
    ├── Models\
    │   ├── Settings.cs            
    │   ├── PanelSettings.cs        # Настройки панели
    │   └── ShortcutItem.cs         # Модель ярлыка (GUID, пути, кэшированная иконка)
    ├── Services\
    │   ├── SettingsService.cs      # Загрузка/сохранение settings.json в %AppData%
    │   ├── ProcessLauncher.cs     # Запуск приложений (включая права администратора)
    │   ├── ShortcutResolver.cs     # Разрешение путей .lnk-файлов через COM-интерфейсы
    │   └── IconExtractor.cs       # Извлечение больших значков (256x256) в кэш PNG
    └── ViewModels\
        ├── MainViewModel.cs       # Управление коллекцией ярлыков и настройками
        └── ShortcutViewModel.cs   # Команды запуска, удаления и администрирования
```

---

## Данные и конфигурация

Конфигурационный файл сохраняется в формате JSON по пути `%AppData%\ShortcutDock\settings.json`:

```json
{
  "PanelSettings": {
    "Position": "Bottom",
    "IconSize": 48,
    "KeepOnTop": true,
    "BackdropType": "Mica",
    "ShowAddButton": true,
    "AutoHide": false,
    "HoverZoom": true,
    "ShowRunningIndicators": true,
    "Language": "ru"
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

Кэш иконок находится в папке `%AppData%\ShortcutDock\Cache\*.png`.

---

## С чего начать

### Требования
- .NET 10.0 SDK или новее

### Сборка и запуск
```powershell
# Клонирование репозитория
git clone https://github.com/Almanex/ShortcutDock.git
cd ShortcutDock

# Восстановление зависимостей и сборка
dotnet build

# Запуск проекта
dotnet run --project src\ShortcutDock
```

### Автономная публикация (Self-Contained EXE)
Для компиляции единого исполняемого файла со всеми встроенными зависимостями:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Готовый исполняемый файл будет сохранен в папке `src\ShortcutDock\bin\Release\net10.0-windows\win-x64\publish\`.

---

## Тестирование
В проекте используется ручное визуальное тестирование интерфейса, а также автоматическая проверка сборки. Чтобы проверить форматирование кода и сборку:
```powershell
dotnet build -c Release
```

---

## Участие в разработке
Вы можете отправлять сообщения об ошибках и пулл-реквесты на GitHub. При внесении значительных изменений рекомендуется сначала создать issue для предварительного обсуждения.

---

## Версионирование
Проект использует систему SemVer. Доступные версии и теги можно посмотреть на странице релизов репозитория.

---

## Авторы и благодарности
- Almanex - Разработчик и первоначальная реализация.
- Сообщество WPF-UI за современные элементы Fluent Design.

---

## Лицензия
Этот проект распространяется под лицензией MIT — подробности смотрите в файле `LICENSE`.