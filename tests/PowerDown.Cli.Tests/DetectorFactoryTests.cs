using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Services;
using PowerDown.Platform.Windows;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Cli.Tests;

public class DetectorFactoryTests
{
    private readonly ConsoleLogger _logger;

    public DetectorFactoryTests()
    {
        _logger = new ConsoleLogger();
    }

    [Fact]
    public void CreateDetectors_WithBothEnabled_ReturnsBothDetectors()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = true
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().HaveCount(2);
        detectors.Should().Contain(d => d.LauncherName == "Steam");
        detectors.Should().Contain(d => d.LauncherName == "Epic Games");
    }

    [Fact]
    public void CreateDetectors_WithSteamOnly_ReturnsSteamDetector()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = false
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().HaveCount(1);
        detectors.Should().Contain(d => d.LauncherName == "Steam");
        detectors.Should().NotContain(d => d.LauncherName == "Epic Games");
    }

    [Fact]
    public void CreateDetectors_WithEpicOnly_ReturnsEpicDetector()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = false,
            MonitorEpic = true
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().HaveCount(1);
        detectors.Should().Contain(d => d.LauncherName == "Epic Games");
        detectors.Should().NotContain(d => d.LauncherName == "Steam");
    }

    [Fact]
    public void CreateDetectors_WithBothDisabled_ReturnsEmptyList()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = false,
            MonitorEpic = false
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetectors_WithCustomSteamPath_PassesPathToDetector()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector(null);
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = false,
            CustomSteamPath = "/custom/steam/path"
        };

        factory.CreateDetectors(config, _logger);

        steamDetector.CapturedPaths.Should().Contain("/custom/steam/path");
    }

    [Fact]
    public void CreateDetectors_WithCustomEpicPath_PassesPathToDetector()
    {
        var steamDetector = new MockSteamPathDetector(null);
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = false,
            MonitorEpic = true,
            CustomEpicPath = "/custom/epic/path"
        };

        factory.CreateDetectors(config, _logger);

        epicDetector.CapturedPaths.Should().Contain("/custom/epic/path");
    }

    [Fact]
    public void CreateDetectors_WithNullConfig_ThrowsArgumentNullException()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        Action act = () => factory.CreateDetectors(null!, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateDetectors_WithNoSteamPathFound_SkipsSteamDetector()
    {
        var steamDetector = new MockSteamPathDetector(null);
        var epicDetector = new MockEpicPathDetector("/mock/epic");
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = true
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().HaveCount(1);
        detectors.Should().Contain(d => d.LauncherName == "Epic Games");
        detectors.Should().NotContain(d => d.LauncherName == "Steam");
    }

    [Fact]
    public void CreateDetectors_WithNoEpicPathFound_SkipsEpicDetector()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector(null);
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = true
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().HaveCount(1);
        detectors.Should().Contain(d => d.LauncherName == "Steam");
        detectors.Should().NotContain(d => d.LauncherName == "Epic Games");
    }

    [Fact]
    public void CreateDetectors_WithNeitherPathFound_ReturnsEmpty()
    {
        var steamDetector = new MockSteamPathDetector(null);
        var epicDetector = new MockEpicPathDetector(null);
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = true
        };

        var detectors = factory.CreateDetectors(config, _logger);

        detectors.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetectors_WithCustomSteamPathNull_UsesAutoDetection()
    {
        var steamDetector = new MockSteamPathDetector("/mock/steam");
        var epicDetector = new MockEpicPathDetector(null);
        var factory = new DetectorFactory(steamDetector, epicDetector);

        var config = new Configuration
        {
            MonitorSteam = true,
            MonitorEpic = false,
            CustomSteamPath = null
        };

        factory.CreateDetectors(config, _logger);

        steamDetector.CapturedPaths.Should().Contain((string?)null);
    }

    #region Mock Classes

    private class MockSteamPathDetector : ISteamPathDetector
    {
        private readonly string? _returnPath;
        private readonly List<string?> _capturedPaths = new();

        public MockSteamPathDetector(string? returnPath)
        {
            _returnPath = returnPath;
        }

        public IReadOnlyList<string?> CapturedPaths => _capturedPaths;

        public string? DetectSteamPath(string? customPath)
        {
            _capturedPaths.Add(customPath);
            return _returnPath;
        }
    }

    private class MockEpicPathDetector : IEpicPathDetector
    {
        private readonly string? _returnPath;
        private readonly List<string?> _capturedPaths = new();

        public MockEpicPathDetector(string? returnPath)
        {
            _returnPath = returnPath;
        }

        public IReadOnlyList<string?> CapturedPaths => _capturedPaths;

        public string? DetectEpicPath(string? customPath)
        {
            _capturedPaths.Add(customPath);
            return _returnPath;
        }
    }

    #endregion
}

public class ConfigurationTests
{
    [Fact]
    public void DefaultConfiguration_HasCorrectValues()
    {
        var config = new Configuration();

        config.VerificationDelaySeconds.Should().Be(60);
        config.PollingIntervalSeconds.Should().Be(10);
        config.RequiredNoActivityChecks.Should().Be(3);
        config.MonitorSteam.Should().BeTrue();
        config.MonitorEpic.Should().BeTrue();
        config.DryRun.Should().BeFalse();
        config.Verbose.Should().BeFalse();
        config.CustomSteamPath.Should().BeNull();
        config.CustomEpicPath.Should().BeNull();
    }

    [Fact]
    public void Configuration_CanSetAllProperties()
    {
        var config = new Configuration
        {
            VerificationDelaySeconds = 120,
            PollingIntervalSeconds = 15,
            RequiredNoActivityChecks = 5,
            MonitorSteam = false,
            MonitorEpic = true,
            DryRun = true,
            Verbose = true,
            CustomSteamPath = "/custom/steam",
            CustomEpicPath = "/custom/epic"
        };

        config.VerificationDelaySeconds.Should().Be(120);
        config.PollingIntervalSeconds.Should().Be(15);
        config.RequiredNoActivityChecks.Should().Be(5);
        config.MonitorSteam.Should().BeFalse();
        config.MonitorEpic.Should().BeTrue();
        config.DryRun.Should().BeTrue();
        config.Verbose.Should().BeTrue();
        config.CustomSteamPath.Should().Be("/custom/steam");
        config.CustomEpicPath.Should().Be("/custom/epic");
    }
}
