using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShortcutDock.Models;

namespace ShortcutDock.Services;

/// <summary>
/// Загрузка/сохранение settings.json в %AppData%\ShortcutDock.
/// Все пути в JSON могут содержать %AppData% — раскрываются при чтении (GetExpandedPath),
/// нормализуются обратно при записи (MakePortable).
/// </summary>
public sealed class SettingsService
{
    public static string AppDataFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShortcutDock");

    public static string SettingsPath { get; } = Path.Combine(AppDataFolder, "settings.json");

    public static string CacheFolder { get; } = Path.Combine(AppDataFolder, "Cache");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string AppDataEnv = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    /// <summary>Заменяет абсолютный путь %AppData% на литерал "%AppData%" для портативности.</summary>
    public static string MakePortable(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.StartsWith(AppDataEnv, StringComparison.OrdinalIgnoreCase)
            ? "%AppData%" + path[AppDataEnv.Length..]
            : path;
    }

    /// <summary>Раскрывает %AppData% и другие переменные окружения в пути.</summary>
    public static string GetExpandedPath(string path) =>
        Environment.ExpandEnvironmentVariables(path ?? string.Empty);

    public Settings Load()
    {
        try
        {
            Directory.CreateDirectory(AppDataFolder);
            if (!File.Exists(SettingsPath))
                return CreateDefaultSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
            return settings ?? CreateDefaultSettings();
        }
        catch
        {
            // Повреждённый конфиг не должен крашить приложение — стартуем с дефолтами.
            return CreateDefaultSettings();
        }
    }

    public Settings CreateDefaultSettings()
    {
        var settings = new Settings();

        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);

        var explorerPath = Path.Combine(winDir, "explorer.exe");
        var notepadPath = Path.Combine(sysDir, "notepad.exe");
        var calcPath = Path.Combine(sysDir, "calc.exe");

        if (File.Exists(explorerPath))
        {
            settings.Shortcuts.Add(new ShortcutItem
            {
                Name = "Explorer",
                TargetPath = explorerPath
            });
        }

        if (File.Exists(notepadPath))
        {
            settings.Shortcuts.Add(new ShortcutItem
            {
                Name = "Notepad",
                TargetPath = notepadPath
            });
        }

        if (File.Exists(calcPath))
        {
            settings.Shortcuts.Add(new ShortcutItem
            {
                Name = "Calculator",
                TargetPath = calcPath
            });
        }

        return settings;
    }

    public void Save(Settings settings)
    {
        Directory.CreateDirectory(AppDataFolder);
        // Нормализуем пути иконок в портативный вид (%AppData%).
        foreach (var s in settings.Shortcuts)
        {
            s.IconPath = MakePortable(s.IconPath);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
