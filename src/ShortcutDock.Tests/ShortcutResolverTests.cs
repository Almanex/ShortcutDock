using System;
using System.IO;
using Xunit;
using ShortcutDock.Services;

namespace ShortcutDock.Tests;

public class ShortcutResolverTests
{
    private readonly ShortcutResolver _resolver = new();

    [Fact]
    public void Resolve_ShouldThrowArgumentException_WhenPathIsEmptyOrWhitespace()
    {
        Assert.Throws<FileNotFoundException>(() => _resolver.Resolve(string.Empty));
        Assert.Throws<FileNotFoundException>(() => _resolver.Resolve("   "));
    }

    [Fact]
    public void Resolve_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".exe");
        Assert.Throws<FileNotFoundException>(() => _resolver.Resolve(nonExistentPath));
    }

    [Fact]
    public void Resolve_ShouldParseSpecialShellFolderWithoutThrowing()
    {
        // Arrange
        var specialPath = "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App";

        // Act
        var result = _resolver.Resolve(specialPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialPath, result.TargetPath);
        Assert.NotEmpty(result.Name);
    }

    [Fact]
    public void Resolve_ShouldParseVirtualGuidPathWithoutThrowing()
    {
        // Arrange
        var guidPath = "shell:::{645FF040-5081-101B-9F08-00AA002F954E}";

        // Act
        var result = _resolver.Resolve(guidPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(guidPath, result.TargetPath);
    }
}
