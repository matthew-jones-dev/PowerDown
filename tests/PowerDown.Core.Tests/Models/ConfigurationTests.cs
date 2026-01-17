using Xunit;
using FluentAssertions;
using PowerDown.Abstractions;

namespace PowerDown.Core.Tests.Models;

public class ConfigurationTests
{
    [Fact]
    public void Configuration_DefaultValues_AreCorrect()
    {
        var config = new Configuration();

        config.VerificationDelaySeconds.Should().Be(120);
        config.PollingIntervalSeconds.Should().Be(15);
        config.RequiredNoActivityChecks.Should().Be(5);
        config.MonitorSteam.Should().BeTrue();
        config.DryRun.Should().BeFalse();
        config.Verbose.Should().BeFalse();
        config.CustomSteamPath.Should().BeNull();
    }

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(180)]
    [InlineData(300)]
    public void Configuration_VerificationDelaySeconds_CanBeSet(int delay)
    {
        var config = new Configuration { VerificationDelaySeconds = delay };
        config.VerificationDelaySeconds.Should().Be(delay);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(30)]
    public void Configuration_PollingIntervalSeconds_CanBeSet(int interval)
    {
        var config = new Configuration { PollingIntervalSeconds = interval };
        config.PollingIntervalSeconds.Should().Be(interval);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Configuration_VerificationDelaySeconds_ThrowsOnInvalid(int invalidDelay)
    {
        var config = new Configuration();
        Action act = () => config.VerificationDelaySeconds = invalidDelay;
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Verification delay must be greater than 0*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Configuration_PollingIntervalSeconds_ThrowsOnInvalid(int invalidInterval)
    {
        var config = new Configuration();
        Action act = () => config.PollingIntervalSeconds = invalidInterval;
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Polling interval must be greater than 0*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Configuration_RequiredNoActivityChecks_ThrowsOnInvalid(int invalidChecks)
    {
        var config = new Configuration();
        Action act = () => config.RequiredNoActivityChecks = invalidChecks;
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Required checks must be greater than 0*");
    }
}
