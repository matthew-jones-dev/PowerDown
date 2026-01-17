using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using Moq;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;
using PowerDown.Core;
using PowerDown.UI.ViewModels;
using Xunit;
using FluentAssertions;

namespace PowerDown.UI.Tests;

public class MainViewModelTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IStatusNotifier> _mockStatusNotifier;

    public MainViewModelTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockStatusNotifier = new Mock<IStatusNotifier>();
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var vm = CreateViewModel();

        vm.MonitorSteam.Should().BeTrue();
        vm.VerificationDelaySeconds.Should().Be(120);
        vm.PollingIntervalSeconds.Should().Be(15);
        vm.RequiredNoActivityChecks.Should().Be(5);
        vm.ShutdownDelaySeconds.Should().Be(60);
        vm.DryRun.Should().BeFalse();
    }

    [Fact]
    public void StartCommand_CanExecute_WhenNotMonitoring()
    {
        var vm = CreateViewModel();

        vm.CanStart.Should().BeTrue();
        vm.CanCancel.Should().BeFalse();
        vm.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void StartCommand_CannotExecute_WhenMonitoring()
    {
        var vm = CreateViewModel();
        vm.StartCommand.Execute(null);

        vm.IsMonitoring.Should().BeTrue();
        vm.CanStart.Should().BeFalse();
        vm.CanCancel.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_CanExecute_WhenMonitoring()
    {
        var vm = CreateViewModel();
        vm.StartCommand.Execute(null);
        vm.CancelCommand.Execute(null);

        vm.IsMonitoring.Should().BeFalse();
        vm.CanStart.Should().BeTrue();
        vm.CanCancel.Should().BeFalse();
    }

    [Fact]
    public void MonitorSteam_CanBeToggled()
    {
        var vm = CreateViewModel();

        vm.MonitorSteam = false;
        vm.MonitorSteam.Should().BeFalse();

        vm.MonitorSteam = true;
        vm.MonitorSteam.Should().BeTrue();
    }

    [Fact]
    public void VerificationDelaySeconds_CanBeModified()
    {
        var vm = CreateViewModel();

        vm.VerificationDelaySeconds = 120;
        vm.VerificationDelaySeconds.Should().Be(120);
    }

    [Fact]
    public void PollingIntervalSeconds_CanBeModified()
    {
        var vm = CreateViewModel();

        vm.PollingIntervalSeconds = 30;
        vm.PollingIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public void RequiredNoActivityChecks_CanBeModified()
    {
        var vm = CreateViewModel();

        vm.RequiredNoActivityChecks = 5;
        vm.RequiredNoActivityChecks.Should().Be(5);
    }

    [Fact]
    public void ShutdownDelaySeconds_CanBeModified()
    {
        var vm = CreateViewModel();

        vm.ShutdownDelaySeconds = 60;
        vm.ShutdownDelaySeconds.Should().Be(60);
    }

    [Fact]
    public void DryRun_CanBeToggled()
    {
        var vm = CreateViewModel();

        vm.DryRun = true;
        vm.DryRun.Should().BeTrue();

        vm.DryRun = false;
        vm.DryRun.Should().BeFalse();
    }

    [Fact]
    public void CustomSteamPath_CanBeSet()
    {
        var vm = CreateViewModel();

        var customPath = "/custom/steam/path";
        vm.CustomSteamPath = customPath;
        vm.CustomSteamPath.Should().Be(customPath);
    }

    [Fact]
    public void HasActiveDownloads_ReturnsFalse_WhenEmpty()
    {
        var vm = CreateViewModel();

        vm.HasActiveDownloads.Should().BeFalse();
    }

    [Fact]
    public void CurrentPhaseText_InitiallyReady()
    {
        var vm = CreateViewModel();

        vm.CurrentPhaseText.Should().Be("Ready");
    }

    [Fact]
    public void PhaseDescription_InitiallySet()
    {
        var vm = CreateViewModel();

        vm.PhaseDescription.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StatusIndicatorColor_InitiallyGreen()
    {
        var vm = CreateViewModel();

        vm.StatusIndicatorColor.Should().Be("#4CAF50");
    }

    [Fact]
    public void ShowVerificationProgress_InitiallyFalse()
    {
        var vm = CreateViewModel();

        vm.ShowVerificationProgress.Should().BeFalse();
    }

    [Fact]
    public void ShowShutdownWarning_InitiallyFalse()
    {
        var vm = CreateViewModel();

        vm.ShowShutdownWarning.Should().BeFalse();
    }

    [Fact]
    public void StartCommand_ImplementsICommand()
    {
        var vm = CreateViewModel();

        vm.StartCommand.Should().BeOfType<RelayCommand>();
        vm.StartCommand.Should().BeAssignableTo<ICommand>();
    }

    [Fact]
    public void CancelCommand_ImplementsICommand()
    {
        var vm = CreateViewModel();

        vm.CancelCommand.Should().BeOfType<RelayCommand>();
        vm.CancelCommand.Should().BeAssignableTo<ICommand>();
    }

    [Fact]
    public void StartCommand_RaisesCanExecuteChanged_WhenMonitoringStateChanges()
    {
        var vm = CreateViewModel();
        var canExecuteChangedRaised = false;

        vm.StartCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;
        vm.StartCommand.Execute(null);

        canExecuteChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_RaisesCanExecuteChanged_WhenMonitoringStateChanges()
    {
        var vm = CreateViewModel();
        var canExecuteChangedRaised = false;

        vm.CancelCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;
        vm.StartCommand.Execute(null);
        vm.CancelCommand.Execute(null);

        canExecuteChangedRaised.Should().BeTrue();
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel();
    }
}
