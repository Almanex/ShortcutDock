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

    public string Name => Model.Name;

    /// <summary>Абсолютный путь к PNG/ICO иконки в кэше (раскрытый из %AppData%).</summary>
    [ObservableProperty]
    private string _iconPath;

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
    private void Launch() => _launcher.Start(Model.TargetPath, runAsAdmin: false);

    [RelayCommand]
    private void RunAsAdmin() => _launcher.Start(Model.TargetPath, runAsAdmin: true);

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
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Изображения и значки (*.png;*.ico;*.jpg;*.jpeg)|*.png;*.ico;*.jpg;*.jpeg|Все файлы (*.*)|*.*",
            Title = "Выберите новый значок для приложения"
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
