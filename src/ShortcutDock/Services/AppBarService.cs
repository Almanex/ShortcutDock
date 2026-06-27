using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ShortcutDock.Native;

namespace ShortcutDock.Services;

/// <summary>
/// Регистрирует окно как AppBar (SHAppBarMessage), чтобы другие развёрнутые окна
/// не перекрывали панель, а система резервировала под неё место на рабочем столе.
/// Поддержка: ABM_NEW, ABM_QUERYPOS, ABM_SETPOS, ABM_REMOVE, обработка ABN_POSCHANGED.
/// Поддерживает все 4 стороны экрана: Left, Top, Right, Bottom.
/// </summary>
public sealed class AppBarService
{
    private const int ABE_BOTTOM = 3;

    private const int ABM_NEW = 0x00000000;
    private const int ABM_REMOVE = 0x00000001;
    private const int ABM_QUERYPOS = 0x00000002;
    private const int ABM_SETPOS = 0x00000003;

    private const int ABN_POSCHANGED = 0x0000001;

    private const int ABM_ACTIVATE = 0x00000006;
    private const int ABM_WINDOWPOSCHANGED = 0x00000009;

    private const int WM_ACTIVATE = 0x0006;
    private const int WM_WINDOWPOSCHANGED = 0x0047;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_DISPLAYCHANGE = 0x007E;
 
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    private IntPtr _hwnd = IntPtr.Zero;
    private uint _callbackMessage;
    private bool _registered;
    private HwndSource? _source;
    private int _appBarHeightPx;
    private FrameworkElement? _window;
    private bool _isUpdating;
    private bool _positionUpdatePending;
    private int _edge = ABE_BOTTOM;
    private double _appBarSizeDip;

    // Хранят последние зарезервированные системой координаты для нашего AppBar
    private int _reservedLeft;
    private int _reservedRight;
    private int _reservedTop;
    private int _reservedBottom;

    public bool IsRegistered => _registered;

    /// <summary>Регистрирует окно как AppBar на указанной стороне экрана. Возвращает true при успехе.</summary>
    public bool Register(FrameworkElement window, IntPtr hwnd, double appBarSizeDip, string position)
    {
        _window = window;
        _hwnd = hwnd;
        _appBarSizeDip = appBarSizeDip;

        _edge = position switch
        {
            "Left" => 0,
            "Top" => 1,
            "Right" => 2,
            _ => 3 // "Bottom"
        };

        var src = PresentationSource.FromVisual(window) as HwndSource
                  ?? HwndSource.FromHwnd(hwnd);
        _source = src;
        if (_source?.CompositionTarget is null) return false;

        // Определяем коэффициент масштабирования DPI в зависимости от направления
        double dpiScale = _edge == 0 || _edge == 2 
            ? _source.CompositionTarget.TransformToDevice.M11 
            : _source.CompositionTarget.TransformToDevice.M22;

        _appBarHeightPx = (int)Math.Ceiling(appBarSizeDip * dpiScale);

        _callbackMessage = Win32.RegisterWindowMessage("ShortcutDockAppBar");
        _source.AddHook(WndProc);

        var abd = NewAppBarData(_edge);
        abd.uCallbackMessage = _callbackMessage;
        if (SHAppBarMessage(ABM_NEW, ref abd) == 0)
            return false;

        _registered = true;
        UpdatePosition();
        return true;
    }

    /// <summary>Обновляет параметры существующего AppBar (размер и положение) без перерегистрации.</summary>
    public void UpdateSettings(double appBarSizeDip, string position)
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;

        _edge = position switch
        {
            "Left" => 0,
            "Top" => 1,
            "Right" => 2,
            _ => 3 // "Bottom"
        };

        if (_source?.CompositionTarget != null)
        {
            double dpiScale = _edge == 0 || _edge == 2 
                ? _source.CompositionTarget.TransformToDevice.M11 
                : _source.CompositionTarget.TransformToDevice.M22;

            _appBarHeightPx = (int)Math.Ceiling(appBarSizeDip * dpiScale);
        }
        _appBarSizeDip = appBarSizeDip;

        UpdatePosition();
    }

    /// <summary>Снимает регистрацию AppBar (вызывать при закрытии окна).</summary>
    public void Unregister()
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;

        var abd = NewAppBarData(_edge);
        abd.uCallbackMessage = _callbackMessage;
        SHAppBarMessage(ABM_REMOVE, ref abd);

        _source?.RemoveHook(WndProc);
        _source = null;
        _registered = false;
    }

    /// <summary>Пересчитывает и резервирует область на рабочем столе (вызывается системой при изменении окружения).</summary>
    public void UpdatePosition()
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;

        if (_isUpdating)
        {
            _positionUpdatePending = true;
            return;
        }

        _isUpdating = true;
        try
        {
            do
            {
                _positionUpdatePending = false;
                var abd = NewAppBarData(_edge);

                // Запрашиваем доступную область, уточняем высоту и низ.
                SHAppBarMessage(ABM_QUERYPOS, ref abd);
                
                if (_edge == 3) // ABE_BOTTOM
                    abd.rc.Top = abd.rc.Bottom - _appBarHeightPx;
                else if (_edge == 1) // ABE_TOP
                    abd.rc.Bottom = abd.rc.Top + _appBarHeightPx;
                else if (_edge == 0) // ABE_LEFT
                    abd.rc.Right = abd.rc.Left + _appBarHeightPx;
                else if (_edge == 2) // ABE_RIGHT
                    abd.rc.Left = abd.rc.Right - _appBarHeightPx;

                SHAppBarMessage(ABM_SETPOS, ref abd);

                // Сохраняем выданные системой координаты
                _reservedLeft = abd.rc.Left;
                _reservedRight = abd.rc.Right;
                _reservedTop = abd.rc.Top;
                _reservedBottom = abd.rc.Bottom;

                UpdateWindowPositionInternal();
            } while (_positionUpdatePending);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>Корректирует только положение окна без изменения размера и резервирования места (вызывается при изменении размера содержимого панели WPF).</summary>
    public void UpdateWindowPosition()
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;

        if (_isUpdating)
        {
            _positionUpdatePending = true;
            return;
        }

        _isUpdating = true;
        try
        {
            do
            {
                _positionUpdatePending = false;
                UpdateWindowPositionInternal();
            } while (_positionUpdatePending);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateWindowPositionInternal()
    {
        if (_window == null || _hwnd == IntPtr.Zero) return;

        var src = PresentationSource.FromVisual(_window) as HwndSource ?? HwndSource.FromHwnd(_hwnd);
        if (src?.CompositionTarget == null) return;

        var window = _window as Window;
        if (window == null) return;

        var content = window.Content as FrameworkElement;
        if (content == null) return;

        int leftPx = _reservedLeft;
        int topPx = _reservedTop;
        int widthPx = 0;
        int heightPx = 0;

        double scaleX = src.CompositionTarget.TransformToDevice.M11;
        double scaleY = src.CompositionTarget.TransformToDevice.M22;

        if (_edge == 1 || _edge == 3) // Горизонтальная панель (Top / Bottom)
        {
            content.Measure(new System.Windows.Size(double.PositiveInfinity, _appBarSizeDip));
            double dipWidth = content.DesiredSize.Width;
            if (dipWidth == 0) dipWidth = 400; // fallback
            double dipHeight = _appBarSizeDip;

            widthPx = (int)Math.Ceiling(dipWidth * scaleX);
            heightPx = (int)Math.Ceiling(dipHeight * scaleY);

            leftPx = _reservedLeft + (_reservedRight - _reservedLeft - widthPx) / 2;
        }
        else // Вертикальная панель (Left / Right)
        {
            content.Measure(new System.Windows.Size(_appBarSizeDip, double.PositiveInfinity));
            double dipHeight = content.DesiredSize.Height;
            if (dipHeight == 0) dipHeight = 400; // fallback
            double dipWidth = _appBarSizeDip;

            widthPx = (int)Math.Ceiling(dipWidth * scaleX);
            heightPx = (int)Math.Ceiling(dipHeight * scaleY);

            topPx = _reservedTop + (_reservedBottom - _reservedTop - heightPx) / 2;
        }

        // Проверяем, изменились ли координаты и размеры
        if (GetWindowRect(_hwnd, out var currentRect))
        {
            int currentWidth = currentRect.Right - currentRect.Left;
            int currentHeight = currentRect.Bottom - currentRect.Top;
            if (currentRect.Left == leftPx && currentRect.Top == topPx &&
                currentWidth == widthPx && currentHeight == heightPx)
            {
                // Все параметры совпадают, прерываем перемещение во избежание бесконечного цикла
                return;
            }
        }

        // Перемещаем и масштабируем окно, позволяя изменять размеры (не передаем SWP_NOSIZE)
        SetWindowPos(_hwnd, IntPtr.Zero,
            leftPx, topPx,
            widthPx, heightPx,
            SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)msg == _callbackMessage && (int)wParam == ABN_POSCHANGED)
        {
            UpdatePosition();
        }
        else if (msg == WM_ACTIVATE)
        {
            var abd = NewAppBarData(_edge);
            SHAppBarMessage(ABM_ACTIVATE, ref abd);
        }
        else if (msg == WM_WINDOWPOSCHANGED)
        {
            var abd = NewAppBarData(_edge);
            SHAppBarMessage(ABM_WINDOWPOSCHANGED, ref abd);
        }
        else if (msg == WM_DISPLAYCHANGE || msg == WM_SETTINGCHANGE)
        {
            // Игнорируем системные сообщения, вызванные нашими собственными вызовами в UpdatePosition,
            // чтобы предотвратить бесконечный цикл обратной связи.
            if (!_isUpdating)
            {
                UpdatePosition();
            }
        }
        return IntPtr.Zero;
    }

    private APPBARDATA NewAppBarData(int edge)
    {
        var mon = Win32.MonitorFromWindow(_hwnd, Win32.MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(mon, ref mi);

        var rcProposed = mi.rcMonitor; // Используем rcMonitor вместо rcWork, чтобы избежать накопления сдвигов при повторных вызовах
        if (edge == 3) // ABE_BOTTOM
        {
            rcProposed.Top = rcProposed.Bottom - _appBarHeightPx;
        }
        else if (edge == 1) // ABE_TOP
        {
            rcProposed.Bottom = rcProposed.Top + _appBarHeightPx;
        }
        else if (edge == 0) // ABE_LEFT
        {
            rcProposed.Right = rcProposed.Left + _appBarHeightPx;
        }
        else if (edge == 2) // ABE_RIGHT
        {
            rcProposed.Left = rcProposed.Right - _appBarHeightPx;
        }

        return new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = _hwnd,
            uEdge = edge,
            rc = rcProposed
        };
    }
}
