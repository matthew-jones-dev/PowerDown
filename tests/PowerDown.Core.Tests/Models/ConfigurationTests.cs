using Xunit;
using FluentAssertions;
using PowerDown.Core;

namespace PowerDown.Core.Tests.Models;

public class ConfigurationTests
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
        config.CustomSteamPath.Should().BeNull();
        config.CustomEpicPath.Should().BeNull();
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(300)]
    public void Configuration_VerificationDelaySeconds_CanBeSet(int delay)
    {
        var config = new Configuration { VerificationDelaySeconds = delay };
        config.VerificationDelaySeconds.Should().Be(delay);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void Configuration_PollingIntervalSeconds_CanBeSet(int interval)
    {
        var config = new Configuration { PollingIntervalSeconds = interval };
        config.PollingIntervalSeconds.Should().Be(interval);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Configuration_RequiredNoActivityChecks_CanBeSet(int checks)
    {
        var config = new Configuration { RequiredNoActivityChecks = checks };
        config.RequiredNoActivityChecks.Should().Be(checks);
    }

    [Fact]
    public void Configuration_CustomSteamPath_CanBeSet()
    {
        var customPath = @"D:\CustomSteam";
        var config = new Configuration { CustomSteamPath = customPath };
        config.CustomSteamPath.Should().Be(customPath);
    }

    [Fact]
    public void Configuration_CustomEpicPath_CanBeSet()
    {
        var customPath = @"E:\Epic Games";
        var config = new Configuration { CustomEpicPath = customPath };
        config.CustomEpicPath.Should().Be(customPath);
    }

    [Fact]
    public void Configuration_DryRun_CanBeSet()
    {
        var config = new Configuration { DryRun = true };
        config.DryRun.Should().BeTrue();
    }

    [Fact]
    public void Configuration_Verbose_CanBeSet()
    {
        var config = new Configuration { Verbose = true };
        config.Verbose.Should().BeTrue();
    }
}
