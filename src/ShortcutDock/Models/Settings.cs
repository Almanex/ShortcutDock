using System.Text.Json.Serialization;

namespace ShortcutDock.Models;

/// <summary>
/// Корневой документ settings.json. Структура точно по ТЗ:
/// { "PanelSettings": {...}, "Shortcuts": [ {...}, ... ] }
/// </summary>
public sealed class Settings
{
    [JsonPropertyName("PanelSettings")]
    public PanelSettings PanelSettings { get; set; } = new();

    [JsonPropertyName("Shortcuts")]
    public List<ShortcutItem> Shortcuts { get; set; } = new();
}
