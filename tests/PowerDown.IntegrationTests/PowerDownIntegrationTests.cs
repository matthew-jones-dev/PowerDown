using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Services;
using PowerDown.Platform.Windows;
using PowerDown.Platform.Windows.Detectors;
using PowerDown.Platform.Windows.Services;
using PowerDown.Cli;

namespace PowerDown.IntegrationTests;

public class ServiceRegistrationTests
{
    [Fact]
    public void ServiceCollection_RegistersAllRequiredServices()
    {
        var services = new ServiceCollection();
        RegisterServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var consoleLogger = serviceProvider.GetService<ConsoleLogger>();
        consoleLogger.Should().NotBeNull();

        var configuration = serviceProvider.GetService<Configuration>();
        configuration.Should().NotBeNull();

        var platformDetector = serviceProvider.GetService<WindowsPlatformDetector>();
        platformDetector.Should().NotBeNull();

        var steamPathDetector = serviceProvider.GetService<ISteamPathDetector>();
        steamPathDetector.Should().NotBeNull();

        var epicPathDetector = serviceProvider.GetService<IEpicPathDetector>();
        epicPathDetector.Should().NotBeNull();

        var shutdownService = serviceProvider.GetService<WindowsShutdownService>();
        shutdownService.Should().NotBeNull();

        var shutdownServiceInterface = serviceProvider.GetService<IShutdownService>();
        shutdownServiceInterface.Should().NotBeNull();

        var detectorFactory = serviceProvider.GetService<IDetectorFactory>();
        detectorFactory.Should().NotBeNull();

        var downloadOrchestrator = serviceProvider.GetService<DownloadOrchestrator>();
        downloadOrchestrator.Should().NotBeNull();
    }

    [Fact]
    public void ServiceCollection_WithSingletonRegistration_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ConsoleLogger>();
        services.AddSingleton<Configuration>();
        services.AddSingleton<WindowsPlatformDetector>();
        services.AddSingleton<ISteamPathDetector, SteamPathDetector>();
        services.AddSingleton<IEpicPathDetector, EpicPathDetector>();
        services.AddSingleton<WindowsShutdownService>();
        services.AddSingleton<IShutdownService>(sp => sp.GetRequiredService<WindowsShutdownService>());

        var serviceProvider = services.BuildServiceProvider();

        var logger1 = serviceProvider.GetRequiredService<ConsoleLogger>();
        var logger2 = serviceProvider.GetRequiredService<ConsoleLogger>();

        logger1.Should().BeSameAs(logger2);
    }

    [Fact]
    public void ServiceCollection_Transient_CreatesNewInstances()
    {
        var services = new ServiceCollection();
        RegisterServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var orchestrator1 = serviceProvider.GetRequiredService<DownloadOrchestrator>();
        var orchestrator2 = serviceProvider.GetRequiredService<DownloadOrchestrator>();

        orchestrator1.Should().NotBeSameAs(orchestrator2);
    }

    [Fact]
    public void ServiceCollection_ResolvesIShutdownService()
    {
        var services = new ServiceCollection();
        RegisterServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var shutdownService = serviceProvider.GetRequiredService<IShutdownService>();
        shutdownService.Should().NotBeNull();
        shutdownService.Should().BeOfType<WindowsShutdownService>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ConsoleLogger>();
        services.AddSingleton<Configuration>();
        services.AddSingleton<WindowsPlatformDetector>();
        services.AddSingleton<ISteamPathDetector, SteamPathDetector>();
        services.AddSingleton<IEpicPathDetector, EpicPathDetector>();
        services.AddSingleton<WindowsShutdownService>();
        services.AddSingleton<IShutdownService>(sp => sp.GetRequiredService<WindowsShutdownService>());
        services.AddSingleton<IDetectorFactory, DetectorFactory>();
        services.AddTransient<DownloadOrchestrator>();
    }
}

public class ConfigurationIntegrationTests
{
    [Fact]
    public void Configuration_DefaultValues_AreCorrect()
    {
        var config = new Configuration();

        config.VerificationDelaySeconds.Should().Be(60);
        config.PollingIntervalSeconds.Should().Be(10);
        config.RequiredNoActivityChecks.Should().Be(3);
        config.MonitorSteam.Should().BeTrue();
        config.MonitorEpic.Should().BeTrue();
        config.DryRun.Should().BeFalse();
        config.Verbose.Should().BeFalse();
    }

    [Fact]
    public void Configuration_ModifyingValues_DoesNotAffectOtherInstances()
    {
        var config1 = new Configuration();
        var config2 = new Configuration();

        config1.VerificationDelaySeconds = 120;
        config1.MonitorEpic = false;

        config2.VerificationDelaySeconds.Should().Be(60);
        config2.MonitorEpic.Should().BeTrue();
    }
}

public class PlatformDetectorIntegrationTests
{
    [Fact]
    public void WindowsPlatformDetector_IsSupported_ReturnsTrue_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var detector = new WindowsPlatformDetector();

        detector.IsSupported().Should().BeTrue();
        detector.GetPlatformName().Should().Be("Windows");
    }
}
