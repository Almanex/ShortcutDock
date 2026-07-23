namespace ShortcutDock.ViewModels;

public sealed class FolderItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
}
