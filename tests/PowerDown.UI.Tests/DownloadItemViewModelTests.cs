using System;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;
using PowerDown.UI.ViewModels;
using Xunit;
using FluentAssertions;

namespace PowerDown.UI.Tests;

public class DownloadItemViewModelTests
{
    [Fact]
    public void Constructor_WithUpdate_SetsProperties()
    {
        var update = new DownloadUpdate
        {
            GameName = "Test Game",
            LauncherName = "Steam",
            Progress = 50.0,
            Status = DownloadStatus.Downloading
        };

        var vm = new DownloadItemViewModel(update);

        vm.GameName.Should().Be("Test Game");
        vm.LauncherName.Should().Be("Steam");
        vm.Progress.Should().Be(50.0);
        vm.StatusText.Should().Contain("Downloading");
    }

    [Fact]
    public void UpdateFrom_UpdatesProgress()
    {
        var vm = new DownloadItemViewModel
        {
            GameName = "Test Game",
            LauncherName = "Steam",
            Progress = 25.0
        };

        var update = new DownloadUpdate
        {
            GameName = "Test Game",
            LauncherName = "Steam",
            Progress = 75.0,
            Status = DownloadStatus.Downloading
        };

        vm.UpdateFrom(update);

        vm.Progress.Should().Be(75.0);
    }

    [Fact]
    public void StatusText_ReflectsDownloadStatus()
    {
        var downloadingUpdate = new DownloadUpdate
        {
            GameName = "Game",
            LauncherName = "Steam",
            Progress = 50.0,
            Status = DownloadStatus.Downloading
        };

        var installingUpdate = new DownloadUpdate
        {
            GameName = "Game",
            LauncherName = "Steam",
            Progress = 95.0,
            Status = DownloadStatus.Installing
        };

        var idleUpdate = new DownloadUpdate
        {
            GameName = "Game",
            LauncherName = "Steam",
            Progress = 100.0,
            Status = DownloadStatus.Idle
        };

        var downloadingVm = new DownloadItemViewModel(downloadingUpdate);
        var installingVm = new DownloadItemViewModel(installingUpdate);
        var idleVm = new DownloadItemViewModel(idleUpdate);

        downloadingVm.StatusText.Should().Contain("Downloading");
        installingVm.StatusText.Should().Contain("Installing");
        idleVm.StatusText.Should().Contain("Completed");
    }

    [Fact]
    public void PropertyChanged_RaisesForGameName()
    {
        var vm = new DownloadItemViewModel();
        var propertyChangedRaised = false;
        string? propertyName = null;

        vm.PropertyChanged += (s, e) =>
        {
            propertyChangedRaised = true;
            propertyName = e.PropertyName;
        };

        vm.GameName = "New Game";

        propertyChangedRaised.Should().BeTrue();
        propertyName.Should().Be(nameof(DownloadItemViewModel.GameName));
    }

    [Fact]
    public void PropertyChanged_RaisesForProgress()
    {
        var vm = new DownloadItemViewModel();
        var propertyChangedRaised = false;
        string? propertyName = null;

        vm.PropertyChanged += (s, e) =>
        {
            propertyChangedRaised = true;
            propertyName = e.PropertyName;
        };

        vm.Progress = 75.0;

        propertyChangedRaised.Should().BeTrue();
        propertyName.Should().Be(nameof(DownloadItemViewModel.Progress));
    }

    [Fact]
    public void DefaultValues_AreEmptyOrZero()
    {
        var vm = new DownloadItemViewModel();

        vm.GameName.Should().BeEmpty();
        vm.LauncherName.Should().BeEmpty();
        vm.Progress.Should().Be(0);
        vm.StatusText.Should().Be("Unknown");
    }
}
