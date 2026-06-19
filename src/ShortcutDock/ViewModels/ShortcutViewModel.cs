using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShortcutDock.Models;
using ShortcutDock.Services;

namespace ShortcutDock.ViewModels;

/// <summary>VM одного ярлыка на панели: команда запуска, запуска от админа, удаления.</summary>
public partial class ShortcutViewModel : ObservableObject
{
    private readonly ProcessLauncher _launcher;
    private readonly Action<ShortcutViewModel> _onRemove;

    public ShortcutItem Model { get; }

    public string Name => Model.Name;

    /// <summary>Абсолютный путь к PNG иконки в кэше (раскрытый из %AppData%).</summary>
    [ObservableProperty]
    private string _iconPath;

    public ShortcutViewModel(ShortcutItem model, ProcessLauncher launcher, Action<ShortcutViewModel> onRemove)
    {
        Model = model;
        _launcher = launcher;
        _onRemove = onRemove;
        _iconPath = SettingsService.GetExpandedPath(model.IconPath);
    }

    [RelayCommand]
    private void Launch() => _launcher.Start(Model.TargetPath, runAsAdmin: false);

    [RelayCommand]
    private void RunAsAdmin() => _launcher.Start(Model.TargetPath, runAsAdmin: true);

    [RelayCommand]
    private void Remove() => _onRemove(this);
}
