using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

        // Отслеживаем системную тему и автоматически обновляем тему приложения
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        Wpf.Ui.Appearance.ApplicationThemeManager.Changed += OnThemeChanged;

        Loaded += OnLoaded;
        Closed += OnClosed;
        IsVisibleChanged += OnIsVisibleChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Load();

        StartAutoHideTimer();
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
            e.PropertyName == nameof(MainViewModel.BackdropType) ||
            e.PropertyName == nameof(MainViewModel.AutoHide) ||
            e.PropertyName == nameof(MainViewModel.HoverZoom) ||
            e.PropertyName == nameof(MainViewModel.ShowRunningIndicators))
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
                    if (_appBar != null && _viewModel.KeepOnTop && !_viewModel.AutoHide)
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

                    // Сохраняем нормальные координаты для авто-скрытия
                    _normalTop = Top;
                    _normalLeft = Left;
                    _isSlidOut = false;
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

        // Управляем Immersive Dark Mode для DWM динамически в зависимости от текущей темы приложения
        bool isDark = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;
        int darkMode = isDark ? 1 : 0;
        Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

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

    public Rect GetCurrentMonitorWorkArea()
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

        if (width == 0 || double.IsNaN(width) || height == 0 || double.IsNaN(height))
        {
            var content = Content as FrameworkElement;
            if (content != null)
            {
                width = isVertical ? (MinWidth > 0 ? MinWidth : content.DesiredSize.Width) : content.DesiredSize.Width;
                height = isVertical ? content.DesiredSize.Height : (MinHeight > 0 ? MinHeight : content.DesiredSize.Height);
            }
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

    private void OnClosed(object? sender, EventArgs e)
    {
        Wpf.Ui.Appearance.ApplicationThemeManager.Changed -= OnThemeChanged;
        _appBar?.Unregister();
    }

    private void OnThemeChanged(Wpf.Ui.Appearance.ApplicationTheme currentTheme, System.Windows.Media.Color systemAccent)
    {
        // Переприменяем размытие с новыми параметрами темной/светлой темы
        ApplyBackdrop(_viewModel.BackdropType);
    }

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
                if (shortcutVM != null)
                {
                    var expandedPath = SettingsService.GetExpandedPath(shortcutVM.Model.TargetPath);
                    if (!shortcutVM.IsRecycleBin && System.IO.Directory.Exists(expandedPath))
                    {
                        _viewModel.OpenFolderFan(shortcutVM, border);
                        return;
                    }

                    if (shortcutVM.LaunchCommand.CanExecute(null))
                    {
                        shortcutVM.LaunchCommand.Execute(null);

                        // Запуск анимации отскока (Bounce)
                        var sb = Resources["BounceStoryboard"] as Storyboard;
                        if (sb != null)
                        {
                            var container = border.FindName("IconImage") as FrameworkElement;
                            if (container != null)
                            {
                                var clone = sb.Clone();
                                clone.Begin(container);
                            }
                        }
                    }
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

    // =========================================================================
    // НОВЫЙ ФУНКЦИОНАЛ: АВТО-СКРЫТИЕ (AUTO-HIDE) И УВЕЛИЧЕНИЕ (HOVER ZOOM)
    // =========================================================================

    private System.Windows.Threading.DispatcherTimer? _autoHideTimer;
    private bool _isSlidOut;
    private double _normalTop;
    private double _normalLeft;
    private bool _isContextMenuOpen;

    private void StartAutoHideTimer()
    {
        _autoHideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        _autoHideTimer.Tick += (s, e) =>
        {
            _autoHideTimer.Stop();
            SlideOut();
        };
    }

    private void SlideIn()
    {
        if (!_viewModel.AutoHide || !_isSlidOut) return;
        _isSlidOut = false;

        AnimateWindowPosition(_normalLeft, _normalTop);
    }

    private void SlideOut()
    {
        if (!_viewModel.AutoHide || _isSlidOut || this.IsMouseOver || _isContextMenuOpen) return;
        _isSlidOut = true;

        _normalTop = Top;
        _normalLeft = Left;

        var helper = new WindowInteropHelper(this);
        var mon = Native.Win32.MonitorFromWindow(helper.Handle, Native.Win32.MONITOR_DEFAULTTONEAREST);
        var mi = new Native.Win32.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Native.Win32.MONITORINFO>() };
        
        double targetTop = Top;
        double targetLeft = Left;

        if (Native.Win32.GetMonitorInfo(mon, ref mi))
        {
            var src = PresentationSource.FromVisual(this) as HwndSource ?? HwndSource.FromHwnd(helper.Handle);
            if (src?.CompositionTarget != null)
            {
                double scaleX = src.CompositionTarget.TransformToDevice.M11;
                double scaleY = src.CompositionTarget.TransformToDevice.M22;

                double monLeft = mi.rcMonitor.Left / scaleX;
                double monRight = mi.rcMonitor.Right / scaleX;
                double monTop = mi.rcMonitor.Top / scaleY;
                double monBottom = mi.rcMonitor.Bottom / scaleY;

                double sliver = 2.0; // полоска в 2px на краю экрана для вызова

                if (_viewModel.Position == "Bottom")
                {
                    targetTop = monBottom - sliver;
                }
                else if (_viewModel.Position == "Top")
                {
                    targetTop = monTop - ActualHeight + sliver;
                }
                else if (_viewModel.Position == "Left")
                {
                    targetLeft = monLeft - ActualWidth + sliver;
                }
                else if (_viewModel.Position == "Right")
                {
                    targetLeft = monRight - sliver;
                }
            }
        }

        AnimateWindowPosition(targetLeft, targetTop);
    }

    private void AnimateWindowPosition(double targetLeft, double targetTop)
    {
        var duration = TimeSpan.FromMilliseconds(250);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        if (Math.Abs(Left - targetLeft) > 0.1)
        {
            var anim = new DoubleAnimation(targetLeft, duration) { EasingFunction = ease };
            anim.Completed += (s, e) =>
            {
                this.BeginAnimation(Window.LeftProperty, null);
                this.Left = targetLeft;
            };
            this.BeginAnimation(Window.LeftProperty, anim);
        }

        if (Math.Abs(Top - targetTop) > 0.1)
        {
            var anim = new DoubleAnimation(targetTop, duration) { EasingFunction = ease };
            anim.Completed += (s, e) =>
            {
                this.BeginAnimation(Window.TopProperty, null);
                this.Top = targetTop;
            };
            this.BeginAnimation(Window.TopProperty, anim);
        }
    }

    private void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Выполняем возврат панели, если она скрыта
        if (_isSlidOut)
        {
            SlideIn();
        }

        _autoHideTimer?.Stop();

        if (!_viewModel.HoverZoom)
        {
            ResetIconScales();
            return;
        }

        var mousePos = e.GetPosition(this);
        var isVertical = _viewModel.PanelOrientation == System.Windows.Controls.Orientation.Vertical;
        var containers = FindVisualChildren<Border>(this).Where(b => b.Name == "IconContainer").ToList();

        foreach (var container in containers)
        {
            try
            {
                var transform = container.TransformToAncestor(this);
                var center = transform.Transform(new System.Windows.Point(container.ActualWidth / 2, container.ActualHeight / 2));

                double distance = isVertical ? Math.Abs(mousePos.Y - center.Y) : Math.Abs(mousePos.X - center.X);

                double maxScale = 1.35;
                double range = 45.0;
                double scale = 1.0 + (maxScale - 1.0) * Math.Exp(-distance * distance / (2.0 * range * range));

                var transformGroup = container.RenderTransform as TransformGroup;
                var scaleTransform = transformGroup?.Children[0] as ScaleTransform;
                if (scaleTransform != null)
                {
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    scaleTransform.ScaleX = scale;
                    scaleTransform.ScaleY = scale;
                }
            }
            catch
            {
                // Игнорируем ошибки при обновлении визуального дерева
            }
        }
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ResetIconScales();

        if (_viewModel.AutoHide && !_isContextMenuOpen)
        {
            _autoHideTimer?.Start();
        }
    }

    private void ResetIconScales()
    {
        var containers = FindVisualChildren<Border>(this).Where(b => b.Name == "IconContainer").ToList();
        foreach (var container in containers)
        {
            var transformGroup = container.RenderTransform as TransformGroup;
            var scaleTransform = transformGroup?.Children[0] as ScaleTransform;
            if (scaleTransform != null && (scaleTransform.ScaleX != 1.0 || scaleTransform.ScaleY != 1.0))
            {
                var animX = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
                var animY = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
            }
        }
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        _isContextMenuOpen = true;
        _autoHideTimer?.Stop();
    }

    private void ContextMenu_Closed(object sender, RoutedEventArgs e)
    {
        _isContextMenuOpen = false;
        if (!this.IsMouseOver && _viewModel.AutoHide)
        {
            _autoHideTimer?.Start();
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T t)
                {
                    yield return t;
                }
                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}
