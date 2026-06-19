using System.Text.Json.Serialization;

namespace ShortcutDock.Models;

/// <summary>
/// Одиночный ярлык на панели. Структура поля соответствует ТЗ (Id/Name/TargetPath/IconPath).
/// </summary>
public sealed class ShortcutItem
{
    /// <summary>Уникальный идентификатор (GUID).</summary>
    [JsonPropertyName("Id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    /// <summary>Отображаемое имя (обычно имя файла .exe без расширения).</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Абсолютный путь к исполняемому файлу (.exe), разрешённый из .lnk при необходимости.</summary>
    [JsonPropertyName("TargetPath")]
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>Путь к кэшированной иконке. Может содержать %AppData% (раскрывается при чтении).</summary>
    [JsonPropertyName("IconPath")]
    public string IconPath { get; set; } = string.Empty;
}
