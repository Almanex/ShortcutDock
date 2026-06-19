using System.Diagnostics;
using System.IO;

namespace ShortcutDock.Services;

/// <summary>
/// Запуск целевого приложения. UseShellExecute=true, как требует ТЗ.
/// Параметр runAsAdmin=true добавляет Verb="runas" (запрос UAC).
/// </summary>
public sealed class ProcessLauncher
{
    public void Start(string targetPath, bool runAsAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(targetPath) || (!File.Exists(targetPath) && !Directory.Exists(targetPath)))
            return;

        var isDir = Directory.Exists(targetPath);
        var psi = new ProcessStartInfo
        {
            FileName = targetPath,
            UseShellExecute = true,
            WorkingDirectory = isDir ? targetPath : (Path.GetDirectoryName(targetPath) ?? string.Empty)
        };

        if (runAsAdmin)
            psi.Verb = "runas";

        Process.Start(psi);
    }
}
