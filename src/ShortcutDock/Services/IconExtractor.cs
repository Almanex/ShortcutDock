using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ShortcutDock.Services;

namespace ShortcutDock.Services;

/// <summary>
/// Извлекает иконку приложения в максимальном разрешении (256x256 JUMBO,
/// с fallback на 48x48 и 32x32), сохраняет как PNG в папке кэша.
/// Используются прямые Win32 P/Invoke (SHGetFileInfo + SHGetImageList + IImageList).
/// </summary>
public sealed class IconExtractor
{
    private const int MAX_PATH = 260;

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_SYSICONINDEX = 0x000004000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    private const int SHIL_LARGE = 0x0;     // 32x32
    private const int SHIL_SMALL = 0x1;     // 16x16
    private const int SHIL_EXTRALARGE = 0x2; // 48x48
    private const int SHIL_JUMBO = 0x4;     // 256x256

    private const int ILD_TRANSPARENT = 0x00000001;
    private static readonly Guid IID_IImageList = new("46EB5926-582E-4017-9FDF-E8998DAA54B6");

    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("shell32.dll")]
    private static extern int SHGetImageList(int iImageList, ref Guid riid, ref IntPtr ppv);

    [DllImport("comctl32.dll", PreserveSig = false)]
    private static extern int ImageList_GetIcon(IntPtr himl, int i, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>Извлекает иконку из exe/dll/папки и сохраняет PNG. Возвращает путь к PNG.</summary>
    public string ExtractToPng(string targetPath)
    {
        if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
            throw new FileNotFoundException("Целевой файл или папка не найдены", targetPath);

        Directory.CreateDirectory(SettingsService.CacheFolder);

        var cleanPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var baseName = Path.GetFileNameWithoutExtension(cleanPath);
        if (string.IsNullOrEmpty(baseName))
            baseName = "folder";

        var cachePath = Path.Combine(SettingsService.CacheFolder,
            $"{baseName}_{targetPath.GetHashCode():X}.png");

        // Если иконка уже в кэше — не извлекаем повторно.
        if (File.Exists(cachePath)) return cachePath;

        using var icon = GetHighResolutionIcon(targetPath)
            ?? SystemIcons.Application;
        using var bmp = icon.ToBitmap();
        bmp.Save(cachePath, ImageFormat.Png);
        return cachePath;
    }

    private static Icon? GetHighResolutionIcon(string targetPath)
    {
        // Получаем индекс иконки файла в системном image list.
        var shfi = new SHFILEINFO();
        SHGetFileInfo(targetPath, FILE_ATTRIBUTE_NORMAL, ref shfi,
            (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_SYSICONINDEX | SHGFI_LARGEICON);

        // Пробуем по порядку: JUMBO (256) -> EXTRALARGE (48).
        foreach (var listId in new[] { SHIL_JUMBO, SHIL_EXTRALARGE })
        {
            if (TryGetIconFromImageList(listId, shfi.iIcon, out var hIcon) && hIcon != IntPtr.Zero)
            {
                try { return Icon.FromHandle(hIcon); }
                catch { DestroyIcon(hIcon); }
            }
        }

        // Финальный fallback: обычная 32x32 иконка через SHGetFileInfo.
        var shfi2 = new SHFILEINFO();
        SHGetFileInfo(targetPath, FILE_ATTRIBUTE_NORMAL, ref shfi2,
            (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);
        if (shfi2.hIcon != IntPtr.Zero)
        {
            try { return Icon.FromHandle(shfi2.hIcon); }
            catch { DestroyIcon(shfi2.hIcon); }
        }
        return null;
    }

    private static bool TryGetIconFromImageList(int listId, int iconIndex, out IntPtr hIcon)
    {
        hIcon = IntPtr.Zero;
        var iid = IID_IImageList;
        var ptr = IntPtr.Zero;
        if (SHGetImageList(listId, ref iid, ref ptr) != 0 || ptr == IntPtr.Zero)
            return false;

        try
        {
            hIcon = ImageList_GetIcon(ptr, iconIndex, ILD_TRANSPARENT);
            return hIcon != IntPtr.Zero;
        }
        finally
        {
            Marshal.Release(ptr);
        }
    }

    [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(IntPtr ppidl, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("shell32.dll")]
    private static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, out IntPtr ppidl);

    public string ExtractRecycleBinIcon()
    {
        Directory.CreateDirectory(SettingsService.CacheFolder);

        bool isFull = RecycleBinService.IsRecycleBinFull();
        var stateName = isFull ? "recycle_full" : "recycle_empty";
        var cachePath = Path.Combine(SettingsService.CacheFolder, $"{stateName}.png");

        if (File.Exists(cachePath)) return cachePath;

        using var icon = GetRecycleBinIconFromSystem() ?? SystemIcons.Application;
        using var bmp = icon.ToBitmap();
        bmp.Save(cachePath, ImageFormat.Png);
        return cachePath;
    }

    private static Icon? GetRecycleBinIconFromSystem()
    {
        IntPtr pidl;
        // CSIDL_BITBUCKET = 10
        if (SHGetSpecialFolderLocation(IntPtr.Zero, 10, out pidl) == 0 && pidl != IntPtr.Zero)
        {
            try
            {
                var shfi = new SHFILEINFO();
                // SHGFI_PIDL = 0x8, SHGFI_SYSICONINDEX = 0x4000
                SHGetFileInfo(pidl, 0, ref shfi, (uint)Marshal.SizeOf<SHFILEINFO>(), 0x8 | 0x4000);

                foreach (var listId in new[] { SHIL_JUMBO, SHIL_EXTRALARGE })
                {
                    if (TryGetIconFromImageList(listId, shfi.iIcon, out var hIcon) && hIcon != IntPtr.Zero)
                    {
                        try { return Icon.FromHandle(hIcon); }
                        catch { DestroyIcon(hIcon); }
                    }
                }

                // Fallback to SHGFI_ICON
                var shfi2 = new SHFILEINFO();
                SHGetFileInfo(pidl, 0, ref shfi2, (uint)Marshal.SizeOf<SHFILEINFO>(), 0x8 | 0x100 | 0x0);
                if (shfi2.hIcon != IntPtr.Zero)
                {
                    try { return Icon.FromHandle(shfi2.hIcon); }
                    catch { DestroyIcon(shfi2.hIcon); }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pidl);
            }
        }
        return null;
    }
}
