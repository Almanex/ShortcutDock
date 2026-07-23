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
    public IconExtractor()
    {
    }

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
    /// <summary>Извлекает иконку из exe/dll/папки и сохраняет PNG. Возвращает путь к PNG.</summary>
    public string ExtractToPng(string targetPath)
    {
        bool isSpecial = targetPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
                         targetPath.Contains(":::{") ||
                         targetPath.Contains("!") ||
                         targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

        if (!isSpecial && !File.Exists(targetPath) && !Directory.Exists(targetPath))
        {
            var errMsg = System.Windows.Application.Current?.TryFindResource("ErrTargetNotFound") as string ?? "Target file or folder not found";
            throw new FileNotFoundException(errMsg, targetPath);
        }

        Directory.CreateDirectory(SettingsService.CacheFolder);

        var cleanPath = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var baseName = Path.GetFileNameWithoutExtension(cleanPath);
        if (string.IsNullOrEmpty(baseName))
            baseName = "app";

        var cachePath = Path.Combine(SettingsService.CacheFolder,
            $"{baseName}_{GetStableHash(targetPath)}.png");

        // Если иконка уже в кэше — не извлекаем повторно.
        if (File.Exists(cachePath)) return cachePath;

        // 1. Пробуем получить чистую 256x256 иконку через IShellItemImageFactory
        if (TryExtractShellItemPng(targetPath, cachePath))
        {
            return cachePath;
        }

        // 2. Если targetPath это .lnk ярлык, пробуем получить иконку целевого файла
        if (targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            var target = ShortcutResolver.ResolveLnkTarget(targetPath);
            if (!string.IsNullOrWhiteSpace(target) && (File.Exists(target) || target.StartsWith("shell:")))
            {
                if (TryExtractShellItemPng(target, cachePath))
                {
                    return cachePath;
                }
            }
        }

        // 3. Fallback: HighResolution Icon (JUMBO 256x256 -> EXTRALARGE 48x48 -> 32x32)
        using var icon = GetHighResolutionIcon(targetPath) ?? SystemIcons.Application;
        if (SaveHIconToPngFile(icon.Handle, cachePath))
        {
            return cachePath;
        }

        using var bmp = icon.ToBitmap();
        bmp.Save(cachePath, ImageFormat.Png);
        return cachePath;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        out IntPtr ppv);

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int cx;
        public int cy;
        public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
    }

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, ref BITMAP lpvObject);

    private static readonly Guid IID_IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    private static readonly Guid IID_IShellItemImageFactory = new("bcc18b79-ba16-442f-80c4-8a59c30c463b");

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(
            [In] SIZE size,
            [In] int flags,
            [Out] out IntPtr phbm);
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private static bool TryExtractShellItemPng(string targetPath, string cachePath)
    {
        var candidates = new List<string> { targetPath };
        if (targetPath.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(targetPath.Substring(@"shell:AppsFolder\".Length));
        }
        else if (targetPath.Contains("!") && !targetPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(@"shell:AppsFolder\" + targetPath);
        }

        foreach (var path in candidates)
        {
            if (TryExtractSingleShellItemPng(path, cachePath))
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryExtractSingleShellItemPng(string parsingName, string cachePath)
    {
        IntPtr pShellItem = IntPtr.Zero;
        IntPtr pFactory = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;

        try
        {
            var iidItem = IID_IShellItem;
            int hr = SHCreateItemFromParsingName(parsingName, IntPtr.Zero, ref iidItem, out pShellItem);
            if (hr != 0 || pShellItem == IntPtr.Zero) return false;

            var iidFactory = IID_IShellItemImageFactory;
            hr = Marshal.QueryInterface(pShellItem, ref iidFactory, out pFactory);
            if (hr != 0 || pFactory == IntPtr.Zero) return false;

            var factory = (IShellItemImageFactory)Marshal.GetObjectForIUnknown(pFactory);

            // SIIGBF_BIGGERSIZEOK = 0x1, SIIGBF_ICONONLY = 0x4 -> 0x5
            hr = factory.GetImage(new SIZE(256, 256), 0x5, out hBitmap);
            if (hr == 0 && hBitmap != IntPtr.Zero)
            {
                return SaveHBitmapToPngFile(hBitmap, cachePath);
            }
        }
        catch { }
        finally
        {
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            if (pFactory != IntPtr.Zero) Marshal.Release(pFactory);
            if (pShellItem != IntPtr.Zero) Marshal.Release(pShellItem);
        }

        return false;
    }

    private static bool SaveHBitmapToPngFile(IntPtr hBitmap, string destinationPath)
    {
        try
        {
            BITMAP bm = new BITMAP();
            GetObject(hBitmap, Marshal.SizeOf(typeof(BITMAP)), ref bm);

            Bitmap? argbBmp = null;
            if (bm.bmBitsPixel == 32 && bm.bmBits != IntPtr.Zero)
            {
                // DIB section: wrap memory directly
                using var bmp = new Bitmap(bm.bmWidth, bm.bmHeight, bm.bmWidthBytes, PixelFormat.Format32bppArgb, bm.bmBits);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY); // DIBs are bottom-up
                argbBmp = new Bitmap(bmp); // Copy to detach from bmBits
            }
            else
            {
                // Fallback (lose alpha)
                using var rawBmp = Image.FromHbitmap(hBitmap);
                argbBmp = new Bitmap(rawBmp.Width, rawBmp.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(argbBmp))
                {
                    g.DrawImage(rawBmp, 0, 0);
                }
            }

            using (argbBmp)
            {
                RemoveSolidWhiteBackgroundIfNeeded(argbBmp);
                using var cropped = CropTransparentMargins(argbBmp);
                cropped.Save(destinationPath, ImageFormat.Png);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool SaveHIconToPngFile(IntPtr hIcon, string destinationPath)
    {
        try
        {
            using var icon = Icon.FromHandle(hIcon);
            using var rawBmp = icon.ToBitmap();
            using var argbBmp = new Bitmap(rawBmp.Width, rawBmp.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(argbBmp))
            {
                g.DrawImage(rawBmp, 0, 0);
            }

            RemoveSolidWhiteBackgroundIfNeeded(argbBmp);
            using var cropped = CropTransparentMargins(argbBmp);
            cropped.Save(destinationPath, ImageFormat.Png);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveSolidWhiteBackgroundIfNeeded(Bitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;
        if (width <= 0 || height <= 0) return;

        var rect = new Rectangle(0, 0, width, height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;

                byte* topLeft = ptr;
                byte* topRight = ptr + (width - 1) * 4;
                byte* bottomLeft = ptr + (height - 1) * stride;
                byte* bottomRight = ptr + (height - 1) * stride + (width - 1) * 4;

                // Проверяем 4 угла: если все они чисто белые (R>240, G>240, B>240, A=255)
                bool isWhiteBg = (topLeft[0] > 240 && topLeft[1] > 240 && topLeft[2] > 240 && topLeft[3] == 255) &&
                                 (topRight[0] > 240 && topRight[1] > 240 && topRight[2] > 240 && topRight[3] == 255) &&
                                 (bottomLeft[0] > 240 && bottomLeft[1] > 240 && bottomLeft[2] > 240 && bottomLeft[3] == 255) &&
                                 (bottomRight[0] > 240 && bottomRight[1] > 240 && bottomRight[2] > 240 && bottomRight[3] == 255);

                if (isWhiteBg)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < width; x++)
                        {
                            byte b = row[x * 4];
                            byte g = row[x * 4 + 1];
                            byte r = row[x * 4 + 2];
                            byte a = row[x * 4 + 3];

                            if (r > 240 && g > 240 && b > 240 && a == 255)
                            {
                                row[x * 4 + 3] = 0; // Делаем прозрачным
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    private static Bitmap CropTransparentMargins(Bitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;

        // Тестируем пороги альфа-канала: от самого чувствительного (10) до высоких (30, 60, 90).
        // Если при низком пороге иконка занимает почти весь холст (из-за мусора/теней на границах),
        // повышаем порог, чтобы найти реальные границы основного изображения.
        int[] thresholds = { 10, 30, 60, 90 };
        int bestMinX = 0, bestMinY = 0, bestMaxX = width - 1, bestMaxY = height - 1;
        bool foundBetter = false;

        foreach (int threshold in thresholds)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            var rect = new Rectangle(0, 0, width, height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* ptr = (byte*)data.Scan0;
                    int stride = data.Stride;

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < width; x++)
                        {
                            byte alpha = row[x * 4 + 3];
                            if (alpha >= threshold)
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                            }
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            if (maxX >= minX && maxY >= minY)
            {
                int cropW = maxX - minX + 1;
                int cropH = maxY - minY + 1;

                // Если обрезанная область стала меньше 85% холста, мы отсекли мусорные полупрозрачные границы!
                if (cropW < width * 0.85 || cropH < height * 0.85)
                {
                    bestMinX = minX;
                    bestMinY = minY;
                    bestMaxX = maxX;
                    bestMaxY = maxY;
                    foundBetter = true;
                    break;
                }

                // В качестве запасного варианта сохраняем результаты первого порога (10)
                if (!foundBetter && threshold == 10)
                {
                    bestMinX = minX;
                    bestMinY = minY;
                    bestMaxX = maxX;
                    bestMaxY = maxY;
                }
            }
        }

        int finalCropW = bestMaxX - bestMinX + 1;
        int finalCropH = bestMaxY - bestMinY + 1;

        // Если даже при высоком пороге изображение занимает весь холст, оставляем как есть
        if (finalCropW >= width * 0.85 && finalCropH >= height * 0.85)
            return (Bitmap)bmp.Clone();

        int maxDim = Math.Max(finalCropW, finalCropH);
        int padding = Math.Max(4, maxDim / 16);
        int finalSize = maxDim + padding * 2;

        var cropped = new Bitmap(finalSize, finalSize, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(cropped))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            int destX = padding + (maxDim - finalCropW) / 2;
            int destY = padding + (maxDim - finalCropH) / 2;

            g.DrawImage(bmp, new Rectangle(destX, destY, finalCropW, finalCropH),
                             new Rectangle(bestMinX, bestMinY, finalCropW, finalCropH),
                             GraphicsUnit.Pixel);
        }

        return cropped;
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

    private static string GetStableHash(string input)
    {
        if (input == null) return "0";
        uint hash = 2166136261;
        foreach (char c in input)
        {
            hash = (hash ^ c) * 16777619;
        }
        return hash.ToString("X8");
    }
}
