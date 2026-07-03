using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShortcutDock.Models;
using ShortcutDock.Services;

namespace ShortcutDock.ViewModels;

/// <summary>
/// Главная VM: список ярлыков, добавление (DnD + диалог «+»), удаление,
/// автосохранение при изменениях.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly ShortcutResolver _resolver;
    private readonly IconExtractor _iconExtractor;
    private readonly ProcessLauncher _launcher;

    public ObservableCollection<ShortcutViewModel> Shortcuts { get; } = new();

    public PanelSettings Panel { get; private set; } = new();

    [ObservableProperty]
    private string _position = "Bottom";

    [ObservableProperty]
    private int _iconSize = 48;

    [ObservableProperty]
    private bool _keepOnTop = true;

    [ObservableProperty]
    private string _backdropType = "None";

    [ObservableProperty]
    private bool _showAddButton = false;

    [ObservableProperty]
    private bool _startWithWindows = false;

    [ObservableProperty]
    private bool _showRecycleBin = false;

    [ObservableProperty]
    private bool _autoHide = false;

    [ObservableProperty]
    private bool _hoverZoom = true;

    [ObservableProperty]
    private bool _showRunningIndicators = true;



    [ObservableProperty]
    private System.Windows.Controls.Orientation _panelOrientation = System.Windows.Controls.Orientation.Horizontal;

    public string IconSizeString
    {
        get => IconSize.ToString();
        set
        {
            if (int.TryParse(value, out int size))
            {
                IconSize = size;
            }
        }
    }

    public MainViewModel(SettingsService settingsService, ShortcutResolver resolver,
                         IconExtractor iconExtractor, ProcessLauncher launcher)
    {
        _settingsService = settingsService;
        _resolver = resolver;
        _iconExtractor = iconExtractor;
        _launcher = launcher;
    }

    /// <summary>Загрузка конфигурации при старте.</summary>
    public void Load()
    {
        var settings = _settingsService.Load();
        Panel = settings.PanelSettings;

        Position = Panel.Position;
        IconSize = Panel.IconSize;
        KeepOnTop = Panel.KeepOnTop;
        BackdropType = Panel.BackdropType ?? "None";
        ShowAddButton = Panel.ShowAddButton;
        StartWithWindows = AutoStartService.IsAutoStartEnabled();
        AutoHide = Panel.AutoHide;
        HoverZoom = Panel.HoverZoom;
        ShowRunningIndicators = Panel.ShowRunningIndicators;
        UpdatePanelOrientation();

        // Clean up old cached Recycle Bin icons to query fresh ones from current system theme
        try
        {
            File.Delete(Path.Combine(SettingsService.CacheFolder, "recycle_empty.png"));
            File.Delete(Path.Combine(SettingsService.CacheFolder, "recycle_full.png"));
        }
        catch {}

        Shortcuts.Clear();
        foreach (var item in settings.Shortcuts)
        {
            // Пропускаем записи с отсутствующей иконкой/целью.
            if (!File.Exists(SettingsService.GetExpandedPath(item.IconPath)))
                continue;
            Shortcuts.Add(new ShortcutViewModel(item, _launcher, Remove, Persist));
        }

        ShowRecycleBin = Panel.ShowRecycleBin;
        UpdateRecycleBinItem();
        StartRecycleBinTimer();
        StartProcessTracker();
    }

    partial void OnPositionChanged(string value)
    {
        Panel.Position = value;
        UpdatePanelOrientation();
        Persist();
    }

    partial void OnIconSizeChanged(int value)
    {
        Panel.IconSize = value;
        OnPropertyChanged(nameof(IconSizeString));
        Persist();
    }

    partial void OnKeepOnTopChanged(bool value)
    {
        Panel.KeepOnTop = value;
        Persist();
    }

    partial void OnBackdropTypeChanged(string value)
    {
        Panel.BackdropType = value;
        Persist();
    }

    partial void OnShowAddButtonChanged(bool value)
    {
        Panel.ShowAddButton = value;
        Persist();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        Panel.StartWithWindows = value;
        AutoStartService.SetAutoStart(value);
        Persist();
    }

    partial void OnShowRecycleBinChanged(bool value)
    {
        Panel.ShowRecycleBin = value;
        UpdateRecycleBinItem();
        Persist();
    }

    partial void OnAutoHideChanged(bool value)
    {
        Panel.AutoHide = value;
        Persist();
    }

    partial void OnHoverZoomChanged(bool value)
    {
        Panel.HoverZoom = value;
        Persist();
    }

    partial void OnShowRunningIndicatorsChanged(bool value)
    {
        Panel.ShowRunningIndicators = value;
        UpdateRunningAppsStatus();
        Persist();
    }


    private void UpdatePanelOrientation()
    {
        PanelOrientation = (Position == "Left" || Position == "Right")
            ? System.Windows.Controls.Orientation.Vertical
            : System.Windows.Controls.Orientation.Horizontal;
    }

    /// <summary>Добавление ярлыка по пути файла (из DnD или диалога).</summary>
    public void AddFromFile(string path)
    {
        try
        {
            var item = _resolver.Resolve(path);
            item.IconPath = _iconExtractor.ExtractToPng(item.TargetPath);

            Shortcuts.Add(new ShortcutViewModel(item, _launcher, Remove, Persist));
            Persist();
            UpdateRunningAppsStatus();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShortcutDock] Не удалось добавить '{path}': {ex.Message}");
        }
    }

    /// <summary>Вызов системного диалога выбора файла (.exe/.lnk).</summary>
    [RelayCommand]
    private void AddViaDialog()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Программы и ярлыки (*.exe;*.lnk)|*.exe;*.lnk|Все файлы (*.*)|*.*",
            Title = "Выберите приложение для добавления на панель"
        };
        if (dlg.ShowDialog() == true)
            AddFromFile(dlg.FileName);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        var win = new SettingsWindow(this);
        win.Owner = System.Windows.Application.Current.MainWindow;
        win.ShowDialog();
    }

    private void Remove(ShortcutViewModel vm)
    {
        Shortcuts.Remove(vm);
        Persist();
    }

    /// <summary>Сохраняет текущее состояние в settings.json.</summary>
    public void Persist()
    {
        var settings = new Settings
        {
            PanelSettings = Panel,
            Shortcuts = Shortcuts.Where(s => !s.IsRecycleBin).Select(s => s.Model).ToList()
        };
        _settingsService.Save(settings);
    }

    private System.Windows.Threading.DispatcherTimer? _recycleBinTimer;

    private void StartRecycleBinTimer()
    {
        _recycleBinTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _recycleBinTimer.Tick += (s, e) => CheckRecycleBinState();
        _recycleBinTimer.Start();
    }

    public void CheckRecycleBinState()
    {
        if (!ShowRecycleBin) return;

        var bin = Shortcuts.FirstOrDefault(s => s.IsRecycleBin);
        if (bin != null)
        {
            var newIconPath = _iconExtractor.ExtractRecycleBinIcon();
            if (bin.IconPath != newIconPath)
            {
                bin.IconPath = newIconPath;
            }
        }
    }

    private void UpdateRecycleBinItem()
    {
        var existing = Shortcuts.FirstOrDefault(s => s.IsRecycleBin);
        if (ShowRecycleBin)
        {
            if (existing == null)
            {
                var item = new ShortcutItem
                {
                    Name = "Корзина",
                    TargetPath = "shell:::{645FF040-5081-101B-9F08-00AA002F954E}",
                    IconPath = _iconExtractor.ExtractRecycleBinIcon()
                };
                Shortcuts.Add(new ShortcutViewModel(item, _launcher, Remove, Persist));
            }
        }
        else
        {
            if (existing != null)
            {
                Shortcuts.Remove(existing);
            }
        }
    }

    private System.Windows.Threading.DispatcherTimer? _processTrackerTimer;

    private void StartProcessTracker()
    {
        _processTrackerTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _processTrackerTimer.Tick += (s, e) => UpdateRunningAppsStatus();
        _processTrackerTimer.Start();
    }

    public void UpdateRunningAppsStatus()
    {
        if (!ShowRunningIndicators)
        {
            foreach (var s in Shortcuts)
            {
                if (s.IsRunning) s.IsRunning = false;
            }
            return;
        }

        try
        {
            var runningPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var processes = System.Diagnostics.Process.GetProcesses();
            foreach (var p in processes)
            {
                try
                {
                    if (p.HasExited) continue;
                    var path = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path))
                    {
                        runningPaths.Add(path);
                    }
                }
                catch
                {
                    // Игнорируем системные процессы
                }
            }

            foreach (var s in Shortcuts)
            {
                if (s.IsRecycleBin) continue;

                var expandedPath = SettingsService.GetExpandedPath(s.Model.TargetPath);
                bool isRunningNow = runningPaths.Contains(expandedPath);
                if (s.IsRunning != isRunningNow)
                {
                    s.IsRunning = isRunningNow;
                }
            }
        }
        catch
        {
            // Безопасность
        }
    }
}
