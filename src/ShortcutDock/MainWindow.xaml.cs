using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ShortcutDock.Services;
using ShortcutDock.ViewModels;

namespace ShortcutDock;

/// <summary>
/// Безрамочное окно Dock-панели на базе стандартного WPF Window.
/// Mica backdrop настраивается вручную через DWM API с поддержкой fallback на Acrylic.
/// Дополнительно: WS_EX_TOOLWINDOW (вне Alt+Tab) и скругление углов.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppBarService? _appBar;

    public MainWindow(MainViewModel viewModel, AppBarService? appBar = null)
    {
        _viewModel = viewModel;
        _appBar = appBar;
        DataContext = viewModel;

        InitializeComponent();

        Loaded += OnLoaded;
        Closed += OnClosed;
        IsVisibleChanged += OnIsVisibleChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Load();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyWindowChromeAndEffects();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        if (IsVisible)
        {
            if (_appBar != null && !_appBar.IsRegistered && _viewModel.KeepOnTop)
            {
                double panelSize = _viewModel.IconSize + 10;
                _appBar.Register(this, hwnd, panelSize, _viewModel.Position);
            }
        }
        else
        {
            if (_appBar != null && _appBar.IsRegistered)
            {
                _appBar.Unregister();
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Position) ||
            e.PropertyName == nameof(MainViewModel.IconSize) ||
            e.PropertyName == nameof(MainViewModel.KeepOnTop) ||
            e.PropertyName == nameof(MainViewModel.BackdropType))
        {
            Dispatcher.BeginInvoke(new Action(ApplySettings));
        }
    }

    private void ApplySettings()
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        Topmost = _viewModel.KeepOnTop;

        var isVertical = _viewModel.PanelOrientation == System.Windows.Controls.Orientation.Vertical;
        double panelSize = _viewModel.IconSize + 10;

        if (isVertical)
        {
            Width = panelSize;
            Height = double.NaN;
            MinHeight = 100;
            MinWidth = panelSize;
            SizeToContent = SizeToContent.Height;
        }
        else
        {
            Height = panelSize;
            Width = double.NaN;
            MinWidth = 100;
            MinHeight = panelSize;
            SizeToContent = SizeToContent.Width;
        }

        ApplyBackdrop(_viewModel.BackdropType);

        if (_appBar != null && _viewModel.KeepOnTop)
        {
            if (_appBar.IsRegistered)
            {
                _appBar.Unregister();
            }
            if (IsVisible)
            {
                _appBar.Register(this, hwnd, panelSize, _viewModel.Position);
            }
        }
        else
        {
            if (_appBar != null && _appBar.IsRegistered)
            {
                _appBar.Unregister();
            }
            CenterWindow();
        }
    }

    private void ApplyBackdrop(string type)
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        int backdropType = type switch
        {
            "Mica" => Native.Win32.DWMSBT_MAINWINDOW,
            "Acrylic" => Native.Win32.DWMSBT_TRANSIENTWINDOW,
            _ => Native.Win32.DWMSBT_NONE
        };

        Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

        var margins = new Native.Win32.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        Native.Win32.DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_appBar != null && _appBar.IsRegistered)
        {
            _appBar.UpdateWindowPosition();
        }
        else
        {
            CenterWindow();
        }
    }

    private void CenterWindow()
    {
        var workArea = SystemParameters.WorkArea;
        var isVertical = _viewModel.PanelOrientation == System.Windows.Controls.Orientation.Vertical;

        if (isVertical)
        {
            Top = workArea.Top + (workArea.Height - ActualHeight) / 2;
            if (_viewModel.Position == "Left")
                Left = workArea.Left + 4;
            else
                Left = workArea.Right - ActualWidth - 4;
        }
        else
        {
            Left = workArea.Left + (workArea.Width - ActualWidth) / 2;
            if (_viewModel.Position == "Top")
                Top = workArea.Top + 4;
            else
                Top = workArea.Bottom - ActualHeight - 4;
        }
    }

    private void OnClosed(object? sender, EventArgs e) => _appBar?.Unregister();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void ApplyWindowChromeAndEffects()
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        var ex = Native.Win32.GetWindowLongPtr(hwnd, Native.Win32.GWL_EXSTYLE).ToInt64();
        ex |= Native.Win32.WS_EX_TOOLWINDOW;
        ex &= ~Native.Win32.WS_EX_APPWINDOW;
        Native.Win32.SetWindowLongPtr(hwnd, Native.Win32.GWL_EXSTYLE, new IntPtr(ex));

        int corner = Native.Win32.DWMWCP_ROUND;
        Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_WINDOW_CORNER_PREFERENCE,
                                           ref corner, sizeof(int));
    }



    private void OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        var allowed = IsFileDrop(e.Data);
        e.Effects = allowed ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!IsFileDrop(e.Data)) return;
        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
        {
            foreach (var file in files)
                _viewModel.AddFromFile(file);
        }
        e.Handled = true;
    }

    private static bool IsFileDrop(System.Windows.IDataObject data) =>
        data.GetDataPresent(System.Windows.DataFormats.FileDrop);
}
