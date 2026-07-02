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
    private System.Windows.Point _dragStartPoint;
    private bool _isMouseDown;
    private bool _isApplyingSettings;

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
        _isApplyingSettings = true;
        try
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

            // Force synchronous layout recalculation so ActualWidth/ActualHeight update immediately
            UpdateLayout();

            ApplyBackdrop(_viewModel.BackdropType);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isApplyingSettings = true;
                try
                {
                    if (_appBar != null && _viewModel.KeepOnTop)
                    {
                        if (IsVisible)
                        {
                            if (!_appBar.IsRegistered)
                            {
                                _appBar.Register(this, hwnd, panelSize, _viewModel.Position);
                            }
                            else
                            {
                                _appBar.UpdateSettings(panelSize, _viewModel.Position);
                            }
                        }
                        else if (_appBar.IsRegistered)
                        {
                            _appBar.Unregister();
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
                finally
                {
                    _isApplyingSettings = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        finally
        {
            _isApplyingSettings = false;
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
        if (_isApplyingSettings) return;

        if (_appBar != null && _appBar.IsRegistered)
        {
            _appBar.UpdateWindowPosition();
        }
        else
        {
            CenterWindow();
        }
    }

    private Rect GetCurrentMonitorWorkArea()
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd != IntPtr.Zero)
        {
            var mon = Native.Win32.MonitorFromWindow(hwnd, Native.Win32.MONITOR_DEFAULTTONEAREST);
            var mi = new Native.Win32.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Native.Win32.MONITORINFO>() };
            if (Native.Win32.GetMonitorInfo(mon, ref mi))
            {
                var src = PresentationSource.FromVisual(this) as HwndSource ?? HwndSource.FromHwnd(hwnd);
                if (src?.CompositionTarget != null)
                {
                    double scaleX = src.CompositionTarget.TransformToDevice.M11;
                    double scaleY = src.CompositionTarget.TransformToDevice.M22;

                    return new Rect(
                        mi.rcWork.Left / scaleX,
                        mi.rcWork.Top / scaleY,
                        (mi.rcWork.Right - mi.rcWork.Left) / scaleX,
                        (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY
                    );
                }
            }
        }
        return SystemParameters.WorkArea;
    }

    private void CenterWindow()
    {
        var workArea = GetCurrentMonitorWorkArea();
        var isVertical = _viewModel.PanelOrientation == System.Windows.Controls.Orientation.Vertical;

        double width = ActualWidth;
        double height = ActualHeight;

        var content = Content as FrameworkElement;
        if (content != null)
        {
            // Используем DesiredSize контента, так как ActualWidth/ActualHeight окна
            // могут быть еще не обновлены ОС после изменения размеров.
            width = isVertical ? (MinWidth > 0 ? MinWidth : content.DesiredSize.Width) : content.DesiredSize.Width;
            height = isVertical ? content.DesiredSize.Height : (MinHeight > 0 ? MinHeight : content.DesiredSize.Height);

            if (width == 0) width = ActualWidth;
            if (height == 0) height = ActualHeight;
        }

        if (width == 0) width = 400; // fallback
        if (height == 0) height = _viewModel.IconSize + 10; // fallback

        if (isVertical)
        {
            Top = workArea.Top + (workArea.Height - height) / 2;
            if (_viewModel.Position == "Left")
                Left = workArea.Left;
            else
                Left = workArea.Right - width;
        }
        else
        {
            Left = workArea.Left + (workArea.Width - width) / 2;
            if (_viewModel.Position == "Top")
                Top = workArea.Top;
            else
                Top = workArea.Bottom - height;
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
        if (IsFileDrop(e.Data))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _isMouseDown = true;
    }

    private void Item_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMouseDown)
        {
            _isMouseDown = false;
            var border = sender as Border;
            if (border != null && border.IsMouseOver)
            {
                var shortcutVM = border.DataContext as ShortcutViewModel;
                if (shortcutVM != null && shortcutVM.LaunchCommand.CanExecute(null))
                {
                    shortcutVM.LaunchCommand.Execute(null);
                }
            }
        }
    }

    private void Item_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isMouseDown && e.LeftButton == MouseButtonState.Pressed)
        {
            System.Windows.Point position = e.GetPosition(null);
            if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isMouseDown = false; // Предотвращаем запуск приложения при отпускании кнопки
                var border = sender as Border;
                var shortcutVM = border?.DataContext as ShortcutViewModel;
                // Запрещаем перетаскивать саму Корзину
                if (shortcutVM != null && !shortcutVM.IsRecycleBin)
                {
                    var data = new System.Windows.DataObject("ShortcutViewModel", shortcutVM);
                    System.Windows.DragDrop.DoDragDrop(border, data, System.Windows.DragDropEffects.Move);
                }
            }
        }
    }

    private void Item_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        var targetData = (sender as FrameworkElement)?.DataContext as ShortcutViewModel;
        if (e.Data.GetDataPresent("ShortcutViewModel") && targetData != null && !targetData.IsRecycleBin)
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            e.Handled = true;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }
    }

    private void Item_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ShortcutViewModel"))
        {
            var droppedData = e.Data.GetData("ShortcutViewModel") as ShortcutViewModel;
            var targetData = (sender as FrameworkElement)?.DataContext as ShortcutViewModel;
            
            if (droppedData != null && targetData != null && droppedData != targetData && !droppedData.IsRecycleBin && !targetData.IsRecycleBin)
            {
                int oldIndex = _viewModel.Shortcuts.IndexOf(droppedData);
                int newIndex = _viewModel.Shortcuts.IndexOf(targetData);
                if (oldIndex >= 0 && newIndex >= 0)
                {
                    _viewModel.Shortcuts.Move(oldIndex, newIndex);
                    _viewModel.Persist();
                }
            }
            e.Handled = true;
        }
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
