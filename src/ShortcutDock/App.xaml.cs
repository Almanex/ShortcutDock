using System.Windows;
using ShortcutDock.Services;
using ShortcutDock.ViewModels;

namespace ShortcutDock;

/// <summary>
/// Точка входа: ручная сборка зависимостей (сервисы -> ViewModel -> MainWindow).
/// Интеграция с системным треем (NotifyIcon).
/// </summary>
public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = new SettingsService();
        var resolver = new ShortcutResolver();
        var iconExtractor = new IconExtractor();
        var launcher = new ProcessLauncher();
        var appBar = new AppBarService();

        _viewModel = new MainViewModel(settings, resolver, iconExtractor, launcher);
        _mainWindow = new MainWindow(_viewModel, appBar);

        InitializeTrayIcon();

        _mainWindow.Show();
    }

    private void InitializeTrayIcon()
    {
        var icon = System.Drawing.SystemIcons.Application;
        try
        {
            var uri = new Uri("pack://application:,,,/app_icon.ico");
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo != null)
            {
                icon = new System.Drawing.Icon(streamInfo.Stream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShortcutDock] Failed to load tray icon: {ex.Message}");
        }

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "ShortcutDock"
        };

        _notifyIcon.DoubleClick += (s, ev) => ToggleVisibility();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();

        var showHideItem = new System.Windows.Forms.ToolStripMenuItem("Показать/Скрыть панель");
        showHideItem.Click += (s, ev) => ToggleVisibility();
        contextMenu.Items.Add(showHideItem);

        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Настройки...");
        settingsItem.Click += (s, ev) => ShowSettings();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Выход");
        exitItem.Click += (s, ev) => ExitApp();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void ToggleVisibility()
    {
        if (_mainWindow == null) return;

        if (_mainWindow.Visibility == Visibility.Visible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    private void ShowSettings()
    {
        if (_viewModel == null || _mainWindow == null) return;

        // Если панель скрыта, показываем её перед открытием настроек
        if (_mainWindow.Visibility != Visibility.Visible)
        {
            _mainWindow.Show();
        }

        _viewModel.ShowSettingsCommand.Execute(null);
    }

    private void ExitApp()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
