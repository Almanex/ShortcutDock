using System.Text.Json.Serialization;

namespace ShortcutDock.Models;

/// <summary>
/// Настройки панели. Поле Position хранится строкой ("Bottom" и т.д.) для совместимости с ТЗ.
/// </summary>
public sealed class PanelSettings
{
    [JsonPropertyName("Position")]
    public string Position { get; set; } = "Bottom";

    [JsonPropertyName("IconSize")]
    public int IconSize { get; set; } = 48;

    [JsonPropertyName("KeepOnTop")]
    public bool KeepOnTop { get; set; } = true;

    [JsonPropertyName("BackdropType")]
    public string BackdropType { get; set; } = "None";

    [JsonPropertyName("ShowAddButton")]
    public bool ShowAddButton { get; set; } = false;

    [JsonPropertyName("StartWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    [JsonPropertyName("ShowRecycleBin")]
    public bool ShowRecycleBin { get; set; } = false;

    [JsonPropertyName("AutoHide")]
    public bool AutoHide { get; set; } = false;

    [JsonPropertyName("HoverZoom")]
    public bool HoverZoom { get; set; } = true;

    [JsonPropertyName("ShowRunningIndicators")]
    public bool ShowRunningIndicators { get; set; } = true;
}
