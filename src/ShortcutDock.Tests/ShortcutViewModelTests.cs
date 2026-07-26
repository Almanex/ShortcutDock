using System;
using Moq;
using Xunit;
using ShortcutDock.Models;
using ShortcutDock.Services;
using ShortcutDock.ViewModels;

namespace ShortcutDock.Tests;

public class ShortcutViewModelTests
{
    private readonly Mock<ProcessLauncher> _mockLauncher;
    private bool _onRemoveCalled;
    private ShortcutViewModel? _onRemoveArg;

    public ShortcutViewModelTests()
    {
        _mockLauncher = new Mock<ProcessLauncher>();
        _onRemoveCalled = false;
    }

    private ShortcutViewModel CreateVm(ShortcutItem item)
    {
        return new ShortcutViewModel(
            item,
            _mockLauncher.Object,
            vm => { _onRemoveCalled = true; _onRemoveArg = vm; },
            () => {}
        );
    }

    [Fact]
    public void IsRecycleBin_ShouldReturnTrue_WhenTargetPathIsRecycleBinGuid()
    {
        // Arrange
        var item = new ShortcutItem
        {
            Name = "Recycle Bin",
            TargetPath = "shell:::{645FF040-5081-101B-9F08-00AA002F954E}"
        };

        // Act
        var vm = CreateVm(item);

        // Assert
        Assert.True(vm.IsRecycleBin);
    }

    [Fact]
    public void IsRecycleBin_ShouldReturnFalse_WhenTargetPathIsNotRecycleBinGuid()
    {
        // Arrange
        var item = new ShortcutItem
        {
            Name = "Notepad",
            TargetPath = "notepad.exe"
        };

        // Act
        var vm = CreateVm(item);

        // Assert
        Assert.False(vm.IsRecycleBin);
    }

    [Fact]
    public void NameProperty_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var item = new ShortcutItem { Name = "OldName", TargetPath = "test.exe" };
        var vm = CreateVm(item);
        string? changedProperty = null;
        vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        // Act
        vm.Name = "NewName";

        // Assert
        Assert.Equal("NewName", vm.Name);
        Assert.Equal("NewName", item.Name);
        Assert.Equal(nameof(ShortcutViewModel.Name), changedProperty);
    }

    [Fact]
    public void LaunchCommand_ShouldInvokeProcessLauncher()
    {
        // Arrange
        var item = new ShortcutItem { Name = "App", TargetPath = "C:\\app.exe" };
        var vm = CreateVm(item);

        // Act
        vm.LaunchCommand.Execute(null);

        // Assert
        _mockLauncher.Verify(l => l.Start("C:\\app.exe", false), Times.Once);
    }

    [Fact]
    public void RunAsAdminCommand_ShouldInvokeProcessLauncherWithAdminFlag()
    {
        // Arrange
        var item = new ShortcutItem { Name = "App", TargetPath = "C:\\app.exe" };
        var vm = CreateVm(item);

        // Act
        vm.RunAsAdminCommand.Execute(null);

        // Assert
        _mockLauncher.Verify(l => l.Start("C:\\app.exe", true), Times.Once);
    }

    [Fact]
    public void OpenFileLocationCommand_ShouldInvokeProcessLauncher_WhenNotRecycleBin()
    {
        // Arrange
        var item = new ShortcutItem { Name = "App", TargetPath = "C:\\app.exe" };
        var vm = CreateVm(item);

        // Act
        vm.OpenFileLocationCommand.Execute(null);

        // Assert
        _mockLauncher.Verify(l => l.OpenLocation("C:\\app.exe"), Times.Once);
    }

    [Fact]
    public void OpenFileLocationCommand_ShouldNotInvokeProcessLauncher_WhenRecycleBin()
    {
        // Arrange
        var item = new ShortcutItem { Name = "Recycle Bin", TargetPath = "shell:::{645FF040-5081-101B-9F08-00AA002F954E}" };
        var vm = CreateVm(item);

        // Act
        vm.OpenFileLocationCommand.Execute(null);

        // Assert
        _mockLauncher.Verify(l => l.OpenLocation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RemoveCommand_ShouldInvokeOnRemoveCallback()
    {
        // Arrange
        var item = new ShortcutItem { Name = "App", TargetPath = "C:\\app.exe" };
        var vm = CreateVm(item);

        // Act
        vm.RemoveCommand.Execute(null);

        // Assert
        Assert.True(_onRemoveCalled);
        Assert.Same(vm, _onRemoveArg);
    }
}
