using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ShortcutDock.Models;

namespace ShortcutDock.Services;

/// <summary>
/// Разрешает исходный путь (файл, .lnk-ярлык) в ShortcutItem:
/// для .lnk программно извлекает целевой .exe через COM (IShellLinkW + IPersistFile),
/// без зависимости от Windows Script Host. Для .exe используется путь напрямую.
/// </summary>
public sealed class ShortcutResolver
{
    // CLSID_ShellLink — стандартный COM-объект для ярлыков Windows.
    private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
    private const int MAX_PATH = 260;

    /// <summary>Создаёт ShortcutItem из перетаскиваемого/выбранного пути.</summary>
    public ShortcutItem Resolve(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            var errMsg = System.Windows.Application.Current?.TryFindResource("ErrFileNotFound") as string ?? "File or folder not found";
            throw new FileNotFoundException(errMsg, sourcePath);
        }

        // Если передано просто имя программы (например "Ubuntu" или "LM Studio"), пытаемся найти ярлык в меню Пуск
        sourcePath = FindShortcutByName(sourcePath);

        bool isUwpOrSpecial = sourcePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase) ||
                              sourcePath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
                              sourcePath.Contains(":::{") ||
                              sourcePath.Contains("!") ||
                              sourcePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

        if (!isUwpOrSpecial && !File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            var errMsg = System.Windows.Application.Current?.TryFindResource("ErrFileNotFound") as string ?? "File or folder not found";
            throw new FileNotFoundException(errMsg, sourcePath);
        }

        string targetPath = sourcePath;
        string name = string.Empty;

        // Если это виртуальный путь shell:AppsFolder или ::: GUID от приложений Microsoft Store
        if (sourcePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase) || sourcePath.Contains(":::{") || sourcePath.Contains("!App"))
        {
            name = GetShellItemDisplayName(sourcePath);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Path.GetFileNameWithoutExtension(sourcePath);
            }
            return new ShortcutItem
            {
                Name = name,
                TargetPath = sourcePath
            };
        }

        var cleanPath = sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        name = Path.GetFileNameWithoutExtension(cleanPath);
        if (string.IsNullOrEmpty(name))
        {
            name = sourcePath;
        }

        if (sourcePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            var sourceName = name;
            targetPath = ResolveLnkTarget(sourcePath);

            // Если GetPath вернул пустую строку (приложения Microsoft Store / UWP) или несуществующий путь,
            // сохраняем сам путь к .lnk ярлыку как рабочий targetPath.
            if (string.IsNullOrWhiteSpace(targetPath) || (!File.Exists(targetPath) && !Directory.Exists(targetPath) && !targetPath.Contains("!App")))
            {
                targetPath = sourcePath;
                name = sourceName;
            }
            else
            {
                var cleanTarget = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var resolvedName = Path.GetFileNameWithoutExtension(cleanTarget);
                if (!string.IsNullOrEmpty(resolvedName))
                {
                    name = resolvedName;
                }
            }
        }

        return new ShortcutItem
        {
            Name = name,
            TargetPath = targetPath,
        };
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    private static readonly Guid IID_IShellItem = new("438269e8-e70a-49e2-be7b-3d22cbe7536e");

    [ComImport]
    [Guid("438269e8-e70a-49e2-be7b-3d22cbe7536e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        [PreserveSig]
        int GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    private const uint SIGDN_NORMALDISPLAY = 0x00000000;

    private static string GetShellItemDisplayName(string parsingName)
    {
        try
        {
            var iid = IID_IShellItem;
            SHCreateItemFromParsingName(parsingName, IntPtr.Zero, ref iid, out var item);
            if (item != null)
            {
                if (item.GetDisplayName(SIGDN_NORMALDISPLAY, out string name) == 0 && !string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }
        catch { }

        return Path.GetFileNameWithoutExtension(parsingName);
    }

    public static string FindShortcutByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        // Если это существующий файл, папка или виртуальный путь
        if (File.Exists(name) || Directory.Exists(name) || name.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            return name;

        try
        {
            string[] startFolders = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs")
            };

            foreach (var folder in startFolders)
            {
                if (Directory.Exists(folder))
                {
                    var files = Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (fileName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                            fileName.StartsWith(name, StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            return file;
                        }
                    }
                }
            }
        }
        catch { }

        return name;
    }

    /// <summary>Извлекает целевой путь из .lnk через COM ShellLink.</summary>
    public static string ResolveLnkTarget(string lnkPath)
    {
        var type = Type.GetTypeFromCLSID(CLSID_ShellLink)
                   ?? throw new InvalidOperationException("ShellLink COM недоступен");
        object shellLink = Activator.CreateInstance(type)
                           ?? throw new InvalidOperationException("Не удалось создать ShellLink");

        try
        {
            var link = (IShellLinkW)shellLink;
            var persist = (IPersistFile)shellLink;

            // STGM_READ = 0
            persist.Load(lnkPath, 0);

            var buffer = new StringBuilder(MAX_PATH);
            var findData = new WIN32_FIND_DATAW();
            // SLGP_RAWPATH = 4 (без попытки найти фактический файл — быстрее и стабильнее)
            link.GetPath(buffer, buffer.Capacity, findData, 4u);

            var resolved = buffer.ToString();
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                resolved = Environment.ExpandEnvironmentVariables(resolved);
            }
            // Если путь пустой (ярлык Microsoft Store / UWP), возвращаем исходный .lnk путь
            return string.IsNullOrWhiteSpace(resolved) ? lnkPath : resolved;
        }
        finally
        {
            Marshal.ReleaseComObject(shellLink);
        }
    }

    // ---- IShellLinkW: точный vtable-порядок из shobjidl_core.h ----
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out] StringBuilder pszFile, int cch, [In, Out] WIN32_FIND_DATAW pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    // ---- IPersistFile: наследует IPersist (GetClassID), порядок важен ----
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private class WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)] public string cFileName = string.Empty;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName = string.Empty;
    }
}
