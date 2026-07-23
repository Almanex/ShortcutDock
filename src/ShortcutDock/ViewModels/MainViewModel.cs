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
    private string _language = "en";

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
        Language = Panel.Language;
        UpdatePanelOrientation();

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
        var newOrientation = (value == "Left" || value == "Right")
            ? System.Windows.Controls.Orientation.Vertical
            : System.Windows.Controls.Orientation.Horizontal;

        int maxAllowed = CalculateMaxShortcutsForCurrentMonitor(IconSize, newOrientation);
        if (Shortcuts.Count > maxAllowed)
        {
            var msgTemplate = System.Windows.Application.Current?.TryFindResource("ErrMaxShortcutsIconSize") as string
                ?? "Cannot set position to {0}. You currently have {1} items, but screen capacity for {0} is max {2} items. Please remove excess items first.";
            var title = System.Windows.Application.Current?.TryFindResource("SettingsHeader") as string ?? "ShortcutDock";
            System.Windows.MessageBox.Show(
                string.Format(msgTemplate, value, Shortcuts.Count, maxAllowed),
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning
            );

            Position = Panel.Position;
            return;
        }

        Panel.Position = value;
        UpdatePanelOrientation();
        Persist();
    }

    public int MaxShortcutsAllowed => CalculateMaxShortcutsForCurrentMonitor(IconSize, PanelOrientation);

    public int GetMaxShortcutsAllowedForSize(int iconSize) => CalculateMaxShortcutsForCurrentMonitor(iconSize, PanelOrientation);

    public int CalculateMaxShortcutsForCurrentMonitor(int iconSize, System.Windows.Controls.Orientation orientation)
    {
        try
        {
            var workArea = System.Windows.Application.Current?.MainWindow is MainWindow mainWin
                ? mainWin.GetCurrentMonitorWorkArea()
                : System.Windows.SystemParameters.WorkArea;

            double availableLength = (orientation == System.Windows.Controls.Orientation.Vertical)
                ? workArea.Height
                : workArea.Width;

            double usableLength = availableLength - 24.0;
            double itemSlotSize = iconSize + 8.0;

            int maxSlots = (int)Math.Floor(usableLength / itemSlotSize);
            int reservedForAddButton = ShowAddButton ? 1 : 0;

            return Math.Max(1, maxSlots - reservedForAddButton);
        }
        catch
        {
            return 15;
        }
    }

    partial void OnIconSizeChanged(int value)
    {
        int maxAllowed = GetMaxShortcutsAllowedForSize(value);
        if (Shortcuts.Count > maxAllowed)
        {
            var msgTemplate = System.Windows.Application.Current?.TryFindResource("ErrMaxShortcutsIconSize") as string
                ?? "Cannot set icon size to {0} px. You currently have {1} items, but the limit for {0} px is {2} items. Please remove excess items first.";
            var title = System.Windows.Application.Current?.TryFindResource("SettingsHeader") as string ?? "ShortcutDock";
            System.Windows.MessageBox.Show(
                string.Format(msgTemplate, value, Shortcuts.Count, maxAllowed),
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning
            );

            IconSize = Panel.IconSize;
            return;
        }

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
        if (value && !Shortcuts.Any(s => s.IsRecycleBin) && Shortcuts.Count >= MaxShortcutsAllowed)
        {
            var msgTemplate = System.Windows.Application.Current?.TryFindResource("ErrMaxShortcutsLimit") as string
                ?? "Maximum limit of items reached for current icon size ({0} px): max {1} items.";
            var title = System.Windows.Application.Current?.TryFindResource("SettingsHeader") as string ?? "ShortcutDock";
            System.Windows.MessageBox.Show(
                string.Format(msgTemplate, IconSize, MaxShortcutsAllowed),
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning
            );

            ShowRecycleBin = false;
            return;
        }

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

    partial void OnLanguageChanged(string value)
    {
        Panel.Language = value;
        App.SetLanguage(value);
        UpdateRecycleBinItem();
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
        if (Shortcuts.Count >= MaxShortcutsAllowed)
        {
            var msgTemplate = System.Windows.Application.Current?.TryFindResource("ErrMaxShortcutsLimit") as string
                ?? "Maximum dock capacity reached for current screen ({0} items at icon size {1} px).";
            var title = System.Windows.Application.Current?.TryFindResource("SettingsHeader") as string ?? "ShortcutDock";
            System.Windows.MessageBox.Show(
                string.Format(msgTemplate, MaxShortcutsAllowed, IconSize),
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning
            );
            return;
        }

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
        var filter = System.Windows.Application.Current.TryFindResource("DlgAddAppFilter") as string ?? "Applications and shortcuts (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*";
        var title = System.Windows.Application.Current.TryFindResource("DlgAddAppTitle") as string ?? "Select an application to add to the dock";
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            Title = title
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
            var binName = System.Windows.Application.Current.TryFindResource("RecycleBinName") as string ?? "Recycle Bin";
            if (existing == null)
            {
                var item = new ShortcutItem
                {
                    Name = binName,
                    TargetPath = "shell:::{645FF040-5081-101B-9F08-00AA002F954E}",
                    IconPath = _iconExtractor.ExtractRecycleBinIcon()
                };
                Shortcuts.Add(new ShortcutViewModel(item, _launcher, Remove, Persist));
            }
            else
            {
                existing.Name = binName;
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
    private bool _isUpdatingProcessStatus;

    private void StartProcessTracker()
    {
        _processTrackerTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.0)
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

        if (_isUpdatingProcessStatus) return;
        _isUpdatingProcessStatus = true;

        var shortcutTargets = Shortcuts
            .Where(s => !s.IsRecycleBin && !string.IsNullOrWhiteSpace(s.Model.TargetPath))
            .Select(s =>
            {
                var expanded = SettingsService.GetExpandedPath(s.Model.TargetPath);
                var exeName = Path.GetFileNameWithoutExtension(expanded);
                return (VM: s, ExpandedPath: expanded, ExeName: exeName);
            })
            .ToList();

        if (shortcutTargets.Count == 0)
        {
            _isUpdatingProcessStatus = false;
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var targetExeNames = new HashSet<string>(
                    shortcutTargets.Select(x => x.ExeName),
                    StringComparer.OrdinalIgnoreCase
                );

                var runningPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processes = System.Diagnostics.Process.GetProcesses();

                foreach (var p in processes)
                {
                    try
                    {
                        if (p.HasExited) continue;

                        if (targetExeNames.Contains(p.ProcessName))
                        {
                            var path = p.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(path))
                            {
                                runningPaths.Add(path);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore system processes
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }

                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        foreach (var target in shortcutTargets)
                        {
                            bool isRunningNow = runningPaths.Contains(target.ExpandedPath);
                            if (target.VM.IsRunning != isRunningNow)
                            {
                                target.VM.IsRunning = isRunningNow;
                            }
                        }
                    }
                    finally
                    {
                        _isUpdatingProcessStatus = false;
                    }
                }));
            }
            catch
            {
                _isUpdatingProcessStatus = false;
            }
        });
    }
}
