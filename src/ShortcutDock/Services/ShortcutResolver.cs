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
        if (string.IsNullOrWhiteSpace(sourcePath) || (!File.Exists(sourcePath) && !Directory.Exists(sourcePath)))
            throw new FileNotFoundException("Файл или папка не найдены", sourcePath);

        string targetPath = sourcePath;
        var cleanPath = sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string name = Path.GetFileNameWithoutExtension(cleanPath);
        if (string.IsNullOrEmpty(name))
        {
            name = sourcePath;
        }

        if (sourcePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            targetPath = ResolveLnkTarget(sourcePath);
            var cleanTarget = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            name = Path.GetFileNameWithoutExtension(cleanTarget);
            if (string.IsNullOrEmpty(name))
            {
                name = targetPath;
            }
        }

        return new ShortcutItem
        {
            Name = name,
            TargetPath = targetPath,
        };
    }

    /// <summary>Извлекает целевой путь из .lnk через COM ShellLink.</summary>
    private static string ResolveLnkTarget(string lnkPath)
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

            return buffer.ToString();
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
