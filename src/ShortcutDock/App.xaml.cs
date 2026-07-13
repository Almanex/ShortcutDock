using System.Windows;
using System.Linq;
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

    private System.Windows.Forms.ToolStripMenuItem? _trayShowHideItem;
    private System.Windows.Forms.ToolStripMenuItem? _traySettingsItem;
    private System.Windows.Forms.ToolStripMenuItem? _trayExitItem;

    private static ResourceDictionary? _languageDictionary;

    public static void SetLanguage(string lang)
    {
        var app = (App)System.Windows.Application.Current;
        var dict = new ResourceDictionary();
        try
        {
            var uri = new Uri($"Resources/Languages/Resources.{lang}.xaml", UriKind.Relative);
            dict.Source = uri;
        }
        catch
        {
            dict.Source = new Uri("Resources/Languages/Resources.ru.xaml", UriKind.Relative);
        }

        if (_languageDictionary != null)
        {
            app.Resources.MergedDictionaries.Remove(_languageDictionary);
        }
        else
        {
            var defaultDict = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Resources/Languages/Resources."));
            if (defaultDict != null)
            {
                app.Resources.MergedDictionaries.Remove(defaultDict);
            }
        }

        _languageDictionary = dict;
        app.Resources.MergedDictionaries.Add(dict);

        app.UpdateTrayMenu();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = new SettingsService();
        var resolver = new ShortcutResolver();
        var iconExtractor = new IconExtractor();
        var launcher = new ProcessLauncher();
        var appBar = new AppBarService();

        var config = settings.Load();
        var lang = config.PanelSettings.Language;
        SetLanguage(lang);

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

        _trayShowHideItem = new System.Windows.Forms.ToolStripMenuItem();
        _trayShowHideItem.Click += (s, ev) => ToggleVisibility();
        contextMenu.Items.Add(_trayShowHideItem);

        _traySettingsItem = new System.Windows.Forms.ToolStripMenuItem();
        _traySettingsItem.Click += (s, ev) => ShowSettings();
        contextMenu.Items.Add(_traySettingsItem);

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        _trayExitItem = new System.Windows.Forms.ToolStripMenuItem();
        _trayExitItem.Click += (s, ev) => ExitApp();
        contextMenu.Items.Add(_trayExitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        UpdateTrayMenu();
    }

    private void UpdateTrayMenu()
    {
        if (_trayShowHideItem != null)
            _trayShowHideItem.Text = TryFindResource("TrayShowHide") as string ?? "Show/Hide Panel";
        if (_traySettingsItem != null)
            _traySettingsItem.Text = TryFindResource("TraySettings") as string ?? "Settings...";
        if (_trayExitItem != null)
            _trayExitItem.Text = TryFindResource("TrayExit") as string ?? "Exit";
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
