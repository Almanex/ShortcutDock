using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShortcutDock.Models;
using ShortcutDock.Services;

namespace ShortcutDock.ViewModels;

/// <summary>VM одного ярлыка на панели: команда запуска, запуска от админа, удаления, смена иконки.</summary>
public partial class ShortcutViewModel : ObservableObject
{
    private readonly ProcessLauncher _launcher;
    private readonly Action<ShortcutViewModel> _onRemove;
    private readonly Action _onChanged;

    public ShortcutItem Model { get; }

    public string Name
    {
        get => Model.Name;
        set
        {
            if (Model.Name != value)
            {
                Model.Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>Абсолютный путь к PNG/ICO иконки в кэше (раскрытый из %AppData%).</summary>
    [ObservableProperty]
    private string _iconPath;

    [ObservableProperty]
    private bool _isRunning = false;

    public ShortcutViewModel(ShortcutItem model, ProcessLauncher launcher, Action<ShortcutViewModel> onRemove, Action onChanged)
    {
        Model = model;
        _launcher = launcher;
        _onRemove = onRemove;
        _onChanged = onChanged;
        _iconPath = SettingsService.GetExpandedPath(model.IconPath);
    }

    public bool IsRecycleBin => Model.TargetPath == "shell:::{645FF040-5081-101B-9F08-00AA002F954E}";

    [RelayCommand]
    private void Launch()
    {
        if (IsRunning && !IsRecycleBin)
        {
            ActivateExistingWindow();
            return;
        }

        _launcher.Start(Model.TargetPath, runAsAdmin: false);

        // Сразу обновляем статус, чтобы индикатор появился быстрее
        if (App.Current.MainWindow?.DataContext is MainViewModel mainVM)
        {
            mainVM.UpdateRunningAppsStatus();
        }
    }

    private void ActivateExistingWindow()
    {
        try
        {
            var expandedPath = SettingsService.GetExpandedPath(Model.TargetPath);
            var exeName = Path.GetFileNameWithoutExtension(expandedPath);
            if (string.IsNullOrEmpty(exeName)) return;

            var processes = System.Diagnostics.Process.GetProcessesByName(exeName);
            foreach (var p in processes)
            {
                try
                {
                    if (p.HasExited) continue;
                    var path = p.MainModule?.FileName;
                    if (string.Equals(path, expandedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var hwnd = p.MainWindowHandle;
                        if (hwnd != IntPtr.Zero)
                        {
                            if (Native.Win32.IsIconic(hwnd))
                            {
                                Native.Win32.ShowWindow(hwnd, Native.Win32.SW_RESTORE);
                            }
                            Native.Win32.SetForegroundWindow(hwnd);
                            return;
                        }
                    }
                }
                catch
                {
                    // Игнорируем процессы без доступа
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private void RunAsAdmin() => _launcher.Start(Model.TargetPath, runAsAdmin: true);

    [RelayCommand]
    private void OpenFileLocation()
    {
        if (IsRecycleBin) return;
        _launcher.OpenLocation(Model.TargetPath);
    }

    [RelayCommand]
    private void Remove() => _onRemove(this);

    [RelayCommand]
    private void EmptyRecycleBin()
    {
        RecycleBinService.EmptyRecycleBin();
        if (App.Current.MainWindow?.DataContext is MainViewModel mainVM)
        {
            mainVM.CheckRecycleBinState();
        }
    }

    [RelayCommand]
    private void ChangeIcon()
    {
        var filter = App.Current.TryFindResource("DlgChangeIconFilter") as string ?? "Images and icons (*.png;*.ico;*.jpg;*.jpeg)|*.png;*.ico;*.jpg;*.jpeg|All files (*.*)|*.*";
        var title = App.Current.TryFindResource("DlgChangeIconTitle") as string ?? "Select a new icon for the application";
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            Title = title
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var selectedPath = dlg.FileName;
                var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShortcutDock", "Cache");
                Directory.CreateDirectory(cacheDir);

                var ext = Path.GetExtension(selectedPath);
                var destName = $"custom_{Guid.NewGuid()}{ext}";
                var destPath = Path.Combine(cacheDir, destName);
                
                File.Copy(selectedPath, destPath, overwrite: true);

                // Записываем портативный путь
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Model.IconPath = destPath.Replace(appDataPath, "%AppData%");
                
                // Обновляем визуальный путь (привязку)
                IconPath = destPath;

                // Вызываем сохранение настроек
                _onChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShortcutDock] Не удалось изменить значок: {ex.Message}");
            }
        }
    }
}
