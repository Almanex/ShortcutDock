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
        UpdatePanelOrientation();

        Shortcuts.Clear();
        foreach (var item in settings.Shortcuts)
        {
            // Пропускаем записи с отсутствующей иконкой/целью.
            if (!File.Exists(SettingsService.GetExpandedPath(item.IconPath)))
                continue;
            Shortcuts.Add(new ShortcutViewModel(item, _launcher, Remove, Persist));
        }
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
            Shortcuts = Shortcuts.Select(s => s.Model).ToList()
        };
        _settingsService.Save(settings);
    }
}
