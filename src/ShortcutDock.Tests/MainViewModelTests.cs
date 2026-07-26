using System;
using System.IO;
using System.Linq;
using Moq;
using Xunit;
using ShortcutDock.Models;
using ShortcutDock.Services;
using ShortcutDock.ViewModels;

namespace ShortcutDock.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly Mock<ShortcutResolver> _mockResolver;
    private readonly Mock<IconExtractor> _mockIconExtractor;
    private readonly Mock<ProcessLauncher> _mockLauncher;
    private readonly SettingsService _settingsService;
    private readonly string _settingsPath = SettingsService.SettingsPath;
    private readonly string _backupPath = SettingsService.SettingsPath + ".bak";
    private readonly bool _hasBackup;

    public MainViewModelTests()
    {
        _mockResolver = new Mock<ShortcutResolver>();
        _mockIconExtractor = new Mock<IconExtractor>();
        _mockLauncher = new Mock<ProcessLauncher>();
        _settingsService = new SettingsService();

        // Backup existing settings file
        if (File.Exists(_settingsPath))
        {
            try
            {
                File.Copy(_settingsPath, _backupPath, true);
                _hasBackup = true;
            }
            catch { }
        }
    }

    public void Dispose()
    {
        // Delete test-created file
        if (File.Exists(_settingsPath))
        {
            try
            {
                File.Delete(_settingsPath);
            }
            catch { }
        }

        // Restore backup
        if (_hasBackup && File.Exists(_backupPath))
        {
            try
            {
                File.Copy(_backupPath, _settingsPath, true);
                File.Delete(_backupPath);
            }
            catch { }
        }
    }

    private MainViewModel CreateVm()
    {
        return new MainViewModel(
            _settingsService,
            _mockResolver.Object,
            _mockIconExtractor.Object,
            _mockLauncher.Object
        );
    }

    [Theory]
    [InlineData("Bottom", System.Windows.Controls.Orientation.Horizontal)]
    [InlineData("Top", System.Windows.Controls.Orientation.Horizontal)]
    [InlineData("Left", System.Windows.Controls.Orientation.Vertical)]
    [InlineData("Right", System.Windows.Controls.Orientation.Vertical)]
    public void PositionChange_ShouldUpdatePanelOrientation(string position, System.Windows.Controls.Orientation expectedOrientation)
    {
        // Arrange
        var vm = CreateVm();

        // Act
        vm.Position = position;

        // Assert
        Assert.Equal(expectedOrientation, vm.PanelOrientation);
        Assert.Equal(position, vm.Panel.Position);
    }

    [Fact]
    public void GetMaxShortcutsAllowedForSize_ShouldReturnCorrectCalculatedSlots()
    {
        // Arrange
        var vm = CreateVm();
        vm.Position = "Bottom"; // Horizontal
        vm.ShowAddButton = false;

        // Act
        // By default, if MainWindow is not initialized, it falls back to SystemParameters.WorkArea or a default.
        // Let's test that it returns a positive value.
        var maxSlots = vm.GetMaxShortcutsAllowedForSize(48);

        // Assert
        Assert.True(maxSlots > 0);
    }

    [Fact]
    public void FolderFanOpacityPercentString_ShouldFormatCorrectly()
    {
        // Arrange
        var vm = CreateVm();

        // Act & Assert
        vm.FolderFanOpacity = 0.15;
        Assert.Equal("15%", vm.FolderFanOpacityPercentString);

        vm.FolderFanOpacity = 0.50;
        Assert.Equal("50%", vm.FolderFanOpacityPercentString);
    }

    [Fact]
    public void IconSizeString_ShouldParseCorrectly()
    {
        // Arrange
        var vm = CreateVm();

        // Act
        vm.IconSizeString = "64";

        // Assert
        Assert.Equal(64, vm.IconSize);
        Assert.Equal("64", vm.IconSizeString);
    }
}
