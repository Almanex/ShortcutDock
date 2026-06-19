using Microsoft.Win32;

namespace ShortcutDock.Services;

/// <summary>
/// Управляет автоматическим запуском ShortcutDock вместе с Windows через реестр (HKCU).
/// </summary>
public static class AutoStartService
{
    private const string KeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ShortcutDock";

    /// <summary>
    /// Включает или выключает автозапуск приложения.
    /// </summary>
    public static void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyName, writable: true);
        if (key == null) return;

        if (enable)
        {
            string path = Environment.ProcessPath ?? "";
            if (!string.IsNullOrEmpty(path))
            {
                key.SetValue(AppName, $"\"{path}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// Проверяет, включен ли автозапуск для приложения.
    /// </summary>
    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyName, writable: false);
        if (key == null) return false;

        return key.GetValue(AppName) != null;
    }
}
