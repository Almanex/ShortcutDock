using System;
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
        if (string.IsNullOrWhiteSpace(targetPath))
            return;

        // Если путь не существует как файл или папка на диске, проверим, не является ли он системной ссылкой (shell:) или URL.
        bool isSpecial = targetPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || 
                         targetPath.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || 
                         targetPath.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
                         targetPath.Contains(":::{");

        if (!isSpecial && !File.Exists(targetPath) && !Directory.Exists(targetPath))
            return;

        try
        {
            var isDir = !isSpecial && Directory.Exists(targetPath);
            string workingDirectory = string.Empty;

            if (!isSpecial)
            {
                try
                {
                    workingDirectory = isDir ? targetPath : (Path.GetDirectoryName(targetPath) ?? string.Empty);
                }
                catch
                {
                    // Игнорируем ошибки извлечения директории для некорректных путей
                }
            }

            var psi = new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true,
                WorkingDirectory = workingDirectory
            };

            if (runAsAdmin)
                psi.Verb = "runas";

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShortcutDock] Не удалось запустить {targetPath}: {ex.Message}");
        }
    }

    public void OpenLocation(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath)) return;

        var expandedPath = SettingsService.GetExpandedPath(targetPath);
        try
        {
            if (File.Exists(expandedPath))
            {
                Process.Start("explorer.exe", $"/select,\"{expandedPath}\"");
            }
            else if (Directory.Exists(expandedPath))
            {
                Process.Start("explorer.exe", $"\"{expandedPath}\"");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShortcutDock] Не удалось открыть расположение '{targetPath}': {ex.Message}");
        }
    }
}
