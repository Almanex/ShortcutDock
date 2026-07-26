using System;
using Xunit;
using ShortcutDock.Services;

namespace ShortcutDock.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void MakePortable_ShouldReplaceAppDataPathWithPlaceholder()
    {
        // Arrange
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var inputPath = System.IO.Path.Combine(appData, "ShortcutDock", "Cache", "test.png");
        var expected = "%AppData%\\ShortcutDock\\Cache\\test.png";

        // Act
        var result = SettingsService.MakePortable(inputPath);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MakePortable_ShouldReturnSamePath_WhenNotUnderAppData()
    {
        // Arrange
        var inputPath = "C:\\Program Files\\App\\icon.ico";

        // Act
        var result = SettingsService.MakePortable(inputPath);

        // Assert
        Assert.Equal(inputPath, result);
    }

    [Fact]
    public void GetExpandedPath_ShouldExpandEnvironmentVariables()
    {
        // Arrange
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var inputPath = "%AppData%\\ShortcutDock\\Cache\\test.png";
        var expected = System.IO.Path.Combine(appData, "ShortcutDock", "Cache", "test.png");

        // Act
        var result = SettingsService.GetExpandedPath(inputPath);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MakePortable_ShouldHandleNullOrEmpty()
    {
        Assert.Null(SettingsService.MakePortable(null!));
        Assert.Equal(string.Empty, SettingsService.MakePortable(string.Empty));
    }
}
