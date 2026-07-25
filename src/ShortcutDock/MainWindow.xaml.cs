using System.Runtime.InteropServices;
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
            e.PropertyName == nameof(MainViewModel.HoverZoom))
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
        _viewModel.RefreshFolderFanBackground();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void ApplyWindowChromeAndEffects()
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        AllowUipiDragDrop(hwnd);

        var ex = Native.Win32.GetWindowLongPtr(hwnd, Native.Win32.GWL_EXSTYLE).ToInt64();
        ex |= Native.Win32.WS_EX_TOOLWINDOW;
        ex &= ~Native.Win32.WS_EX_APPWINDOW;
        Native.Win32.SetWindowLongPtr(hwnd, Native.Win32.GWL_EXSTYLE, new IntPtr(ex));

        int corner = Native.Win32.DWMWCP_ROUND;
        Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_WINDOW_CORNER_PREFERENCE,
                                           ref corner, sizeof(int));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

    private const uint MSGFLT_ALLOW = 1;
    private const uint WM_DROPFILES = 0x0233;
    private const uint WM_COPYDATA = 0x004A;
    private const uint WM_COPYGLOBALDATA = 0x0049;

    private static void AllowUipiDragDrop(IntPtr hwnd)
    {
        try
        {
            ChangeWindowMessageFilterEx(hwnd, WM_DROPFILES, MSGFLT_ALLOW, IntPtr.Zero);
            ChangeWindowMessageFilterEx(hwnd, WM_COPYDATA, MSGFLT_ALLOW, IntPtr.Zero);
            ChangeWindowMessageFilterEx(hwnd, WM_COPYGLOBALDATA, MSGFLT_ALLOW, IntPtr.Zero);
        }
        catch { }
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

    private void OnPreviewDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ShortcutViewModel"))
        {
            e.Effects = System.Windows.DragDropEffects.Move;
        }
        else
        {
            // shell:AppsFolder разрешает ТОЛЬКО Link, Проводник — Copy|Move.
            // Берём первый доступный эффект из того, что разрешил источник.
            if (e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Copy))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else if (e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Link))
                e.Effects = System.Windows.DragDropEffects.Link;
            else if (e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Move))
                e.Effects = System.Windows.DragDropEffects.Move;
            else
                e.Effects = e.AllowedEffects;
        }
        e.Handled = true;
    }

    private void OnPreviewDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ShortcutViewModel")) return;

        // Диагностика: записываем все форматы и данные в лог-файл
        try
        {
            var logPath = System.IO.Path.Combine(SettingsService.CacheFolder, "drop_debug.txt");
            System.IO.Directory.CreateDirectory(SettingsService.CacheFolder);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== DROP at {DateTime.Now:HH:mm:ss} AllowedEffects={e.AllowedEffects} ===");
            sb.AppendLine("Available formats:");
            foreach (var fmt in e.Data.GetFormats())
            {
                sb.Append($"  [{fmt}] ");
                try
                {
                    var obj = e.Data.GetData(fmt);
                    if (obj is string s) sb.Append($"string: \"{s}\"");
                    else if (obj is string[] arr) sb.Append($"string[]: [{string.Join(", ", arr)}]");
                    else if (obj is System.IO.MemoryStream ms) sb.Append($"MemoryStream: {ms.Length} bytes");
                    else if (obj != null) sb.Append($"{obj.GetType().Name}: {obj}");
                    else sb.Append("null");
                }
                catch (Exception ex) { sb.Append($"ERROR: {ex.Message}"); }
                sb.AppendLine();
            }
            var resolvedFiles = GetDroppedFilePaths(e.Data);
            sb.AppendLine($"Resolved paths ({resolvedFiles.Count}):");
            foreach (var f in resolvedFiles) sb.AppendLine($"  -> {f}");
            sb.AppendLine();
            System.IO.File.AppendAllText(logPath, sb.ToString());
        }
        catch { }

        var files = GetDroppedFilePaths(e.Data);
        if (files.Count > 0)
        {
            foreach (var file in files)
            {
                _viewModel.AddFromFile(file);
            }
        }
        else
        {
            // Резервный перебор всех доступных форматов в e.Data
            foreach (var format in e.Data.GetFormats())
            {
                try
                {
                    var dataObj = e.Data.GetData(format);
                    if (dataObj is string strPath && (System.IO.File.Exists(strPath) || System.IO.Directory.Exists(strPath) || strPath.Contains("!App") || strPath.StartsWith("shell:")))
                    {
                        _viewModel.AddFromFile(strPath);
                        break;
                    }
                    else if (dataObj is string[] strArr)
                    {
                        foreach (var path in strArr)
                        {
                            if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path) || path.Contains("!App") || path.StartsWith("shell:"))
                                _viewModel.AddFromFile(path);
                        }
                    }
                }
                catch { }
            }
        }
        e.Handled = true;
    }

    private void Item_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        var targetData = (sender as FrameworkElement)?.DataContext as ShortcutViewModel;
        if (e.Data.GetDataPresent("ShortcutViewModel") && targetData != null && !targetData.IsRecycleBin)
        {
            e.Effects = System.Windows.DragDropEffects.Move;
        }
        else
        {
            if (e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Copy))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else if (e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Link))
                e.Effects = System.Windows.DragDropEffects.Link;
            else if (e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Move))
                e.Effects = System.Windows.DragDropEffects.Move;
            else
                e.Effects = e.AllowedEffects;
        }
        e.Handled = true;
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
        else
        {
            OnPreviewDrop(sender, e);
        }
    }

    private static bool IsFileDrop(System.Windows.IDataObject data) =>
        data.GetDataPresent(System.Windows.DataFormats.FileDrop) ||
        data.GetDataPresent("FileNameW") ||
        data.GetDataPresent("FileName") ||
        data.GetDataPresent("Shell IDList Array") ||
        (data.GetDataPresent("FileGroupDescriptorW") && data.GetDataPresent("FileContents"));

    public static List<string> GetDroppedFilePaths(System.Windows.IDataObject data)
    {
        var rawResult = GetRawDroppedFilePaths(data);
        return NormalizeDroppedPaths(rawResult);
    }

    private static List<string> NormalizeDroppedPaths(List<string> rawPaths)
    {
        var normalized = new List<string>();
        foreach (var p in rawPaths)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            string clean = p.Trim();
            if (!System.IO.File.Exists(clean) && !System.IO.Directory.Exists(clean) && !clean.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            {
                if (clean.Contains("!") || clean.Contains(":::{") || (!clean.Contains("\\") && !clean.Contains("/")))
                {
                    clean = @"shell:AppsFolder\" + clean;
                }
            }
            normalized.Add(clean);
        }
        return normalized;
    }

    private static List<string> GetRawDroppedFilePaths(System.Windows.IDataObject data)
    {
        var result = new List<string>();

        try
        {
            // 1. Стандартный FileDrop (Рабочий стол, Проводник, Меню Пуск AUMID)
            if (data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                if (data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    result.AddRange(files.Where(f => !string.IsNullOrWhiteSpace(f)));
                    if (result.Count > 0) return result;
                }
            }

            // 2. FileNameW / FileName (Ярлыки из меню Пуск)
            if (data.GetDataPresent("FileNameW"))
            {
                if (data.GetData("FileNameW") is string[] filesW)
                {
                    result.AddRange(filesW.Where(f => !string.IsNullOrWhiteSpace(f)));
                    if (result.Count > 0) return result;
                }
                else if (data.GetData("FileNameW") is string singleW && !string.IsNullOrWhiteSpace(singleW))
                {
                    result.Add(singleW);
                    return result;
                }
            }

            if (data.GetDataPresent("FileName"))
            {
                if (data.GetData("FileName") is string[] filesA)
                {
                    result.AddRange(filesA.Where(f => !string.IsNullOrWhiteSpace(f)));
                    if (result.Count > 0) return result;
                }
                else if (data.GetData("FileName") is string singleA && !string.IsNullOrWhiteSpace(singleA))
                {
                    result.Add(singleA);
                    return result;
                }
            }

            // 3. Shell IDList Array (Прямое перетаскивание из меню Пуск и shell:AppsFolder для Microsoft Store)
            if (data.GetDataPresent("Shell IDList Array"))
            {
                if (data.GetData("Shell IDList Array") is System.IO.MemoryStream ms)
                {
                    var pidlPaths = GetPathsFromShellIdListStream(ms);
                    if (pidlPaths.Count > 0)
                    {
                        result.AddRange(pidlPaths);
                        return result;
                    }
                }
            }

            // 4. FileGroupDescriptorW + FileContents (Виртуальные .lnk стримы из меню Пуск Windows 11)
            if (data.GetDataPresent("FileGroupDescriptorW") && data.GetDataPresent("FileContents"))
            {
                var tempPath = ExtractVirtualLnkFromGroupDescriptor(data);
                if (!string.IsNullOrEmpty(tempPath))
                {
                    result.Add(tempPath);
                    return result;
                }
            }

            // 5. UnicodeText / Text
            if (data.GetDataPresent(System.Windows.DataFormats.UnicodeText))
            {
                if (data.GetData(System.Windows.DataFormats.UnicodeText) is string text && !string.IsNullOrWhiteSpace(text))
                {
                    var cleanText = text.Trim('"').Trim();
                    if (System.IO.File.Exists(cleanText) || System.IO.Directory.Exists(cleanText) || cleanText.Contains("!"))
                    {
                        result.Add(cleanText);
                        return result;
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }

        return result;
    }

    private static string? ExtractVirtualLnkFromGroupDescriptor(System.Windows.IDataObject data)
    {
        try
        {
            if (data.GetData("FileGroupDescriptorW") is System.IO.MemoryStream msGroup &&
                data.GetData("FileContents") is System.IO.MemoryStream msContents)
            {
                byte[] groupBytes = msGroup.ToArray();
                if (groupBytes.Length < 80) return null;

                // Читаем имя файла из cFileName структуры FILEDESCRIPTORW (смещение 76)
                string fileName = System.Text.Encoding.Unicode.GetString(groupBytes, 76, 520).Split('\0')[0];
                if (string.IsNullOrWhiteSpace(fileName)) fileName = "app.lnk";

                System.IO.Directory.CreateDirectory(SettingsService.CacheFolder);
                string tempLnkPath = System.IO.Path.Combine(SettingsService.CacheFolder, fileName);
                System.IO.File.WriteAllBytes(tempLnkPath, msContents.ToArray());
                return tempLnkPath;
            }
        }
        catch
        {
            // Fallback
        }
        return null;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetNameFromIDList(IntPtr pidl, uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    private const uint SIGDN_FILESYSPATH = 0x80058000;
    private const uint SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000;

    [DllImport("shell32.dll")]
    private static extern IntPtr ILCombine(IntPtr pidl1, IntPtr pidl2);

    [DllImport("shell32.dll")]
    private static extern void ILFree(IntPtr pidl);

    private static List<string> GetPathsFromShellIdListStream(System.IO.MemoryStream ms)
    {
        var paths = new List<string>();
        byte[] bytes = ms.ToArray();
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            IntPtr basePtr = handle.AddrOfPinnedObject();
            int cidl = Marshal.ReadInt32(basePtr);
            if (cidl <= 0) return paths;

            int[] offsets = new int[cidl + 1];
            for (int i = 0; i <= cidl; i++)
            {
                offsets[i] = Marshal.ReadInt32(basePtr, (i + 1) * 4);
            }

            IntPtr parentPidl = IntPtr.Add(basePtr, offsets[0]);

            for (int i = 1; i <= cidl; i++)
            {
                IntPtr childPidl = IntPtr.Add(basePtr, offsets[i]);
                IntPtr fullPidl = ILCombine(parentPidl, childPidl);
                if (fullPidl != IntPtr.Zero)
                {
                    try
                    {
                        if (SHGetNameFromIDList(fullPidl, SIGDN_FILESYSPATH, out string path) == 0 && !string.IsNullOrWhiteSpace(path))
                        {
                            paths.Add(path);
                        }
                        else if (SHGetNameFromIDList(fullPidl, SIGDN_DESKTOPABSOLUTEPARSING, out string parsingName) == 0 && !string.IsNullOrWhiteSpace(parsingName))
                        {
                            // Если это AUMID (например Microsoft.WindowsCalculator_8wekyb3d8bbwe!App),
                            // а не путь к файлу — добавляем префикс shell:AppsFolder\ для запуска и извлечения иконок.
                            if (!System.IO.File.Exists(parsingName) && !System.IO.Directory.Exists(parsingName) && !parsingName.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
                            {
                                parsingName = @"shell:AppsFolder\" + parsingName;
                            }
                            paths.Add(parsingName);
                        }
                    }
                    finally
                    {
                        ILFree(fullPidl);
                    }
                }
            }
        }
        finally
        {
            handle.Free();
        }
        return paths;
    }

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
