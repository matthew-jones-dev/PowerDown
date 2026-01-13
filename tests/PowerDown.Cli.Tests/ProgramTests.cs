using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PowerDown.Core;
using PowerDown.Cli;

namespace PowerDown.Cli.Tests;

public class ProgramTests
{
    #region Valid Argument Tests

    [Theory]
    [InlineData(new string[] { }, 60, 10, 3, true, true, null, null, false, false)]
    [InlineData(new string[] { "--delay", "120" }, 120, 10, 3, true, true, null, null, false, false)]
    [InlineData(new string[] { "-d", "30" }, 30, 10, 3, true, true, null, null, false, false)]
    [InlineData(new string[] { "--interval", "5" }, 60, 5, 3, true, true, null, null, false, false)]
    [InlineData(new string[] { "-i", "20" }, 60, 20, 3, true, true, null, null, false, false)]
    [InlineData(new string[] { "--checks", "5" }, 60, 10, 5, true, true, null, null, false, false)]
    [InlineData(new string[] { "-c", "1" }, 60, 10, 1, true, true, null, null, false, false)]
    public void ParseArguments_WithValidArguments_ReturnsCorrectConfiguration(
        string[] args,
        int expectedDelay,
        int expectedInterval,
        int expectedChecks,
        bool expectedMonitorSteam,
        bool expectedMonitorEpic,
        string? expectedSteamPath,
        string? expectedEpicPath,
        bool expectedDryRun,
        bool expectedVerbose)
    {
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.VerificationDelaySeconds.Should().Be(expectedDelay);
        result.Config.PollingIntervalSeconds.Should().Be(expectedInterval);
        result.Config.RequiredNoActivityChecks.Should().Be(expectedChecks);
        result.Config.MonitorSteam.Should().Be(expectedMonitorSteam);
        result.Config.MonitorEpic.Should().Be(expectedMonitorEpic);
        result.Config.CustomSteamPath.Should().Be(expectedSteamPath);
        result.Config.CustomEpicPath.Should().Be(expectedEpicPath);
        result.Config.DryRun.Should().Be(expectedDryRun);
        result.Config.Verbose.Should().Be(expectedVerbose);
    }

    [Fact]
    public void ParseArguments_WithSteamOnly_DisablesEpic()
    {
        var args = new[] { "--steam-only" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.MonitorSteam.Should().BeTrue();
        result.Config.MonitorEpic.Should().BeFalse();
    }

    [Fact]
    public void ParseArguments_WithSteamOnlyShort_DisablesEpic()
    {
        var args = new[] { "-s" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.MonitorSteam.Should().BeTrue();
        result.Config.MonitorEpic.Should().BeFalse();
    }

    [Fact]
    public void ParseArguments_WithEpicOnly_DisablesSteam()
    {
        var args = new[] { "--epic-only" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.MonitorSteam.Should().BeFalse();
        result.Config.MonitorEpic.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithEpicOnlyShort_DisablesSteam()
    {
        var args = new[] { "-e" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.MonitorSteam.Should().BeFalse();
        result.Config.MonitorEpic.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithCustomSteamPath_StoresPath()
    {
        var args = new[] { "--steam-path", "/custom/steam" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.CustomSteamPath.Should().Be("/custom/steam");
    }

    [Fact]
    public void ParseArguments_WithCustomEpicPath_StoresPath()
    {
        var args = new[] { "--epic-path", "/custom/epic" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.CustomEpicPath.Should().Be("/custom/epic");
    }

    [Fact]
    public void ParseArguments_WithDryRun_SetsDryRunTrue()
    {
        var args = new[] { "--dry-run" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.DryRun.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithDryRunShort_SetsDryRunTrue()
    {
        var args = new[] { "-r" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.DryRun.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithVerbose_SetsVerboseTrue()
    {
        var args = new[] { "--verbose" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.Verbose.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithVerboseShort_SetsVerboseTrue()
    {
        var args = new[] { "-v" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.Verbose.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithMultipleArguments_AppliesAll()
    {
        var args = new[] {
            "--delay", "90",
            "--interval", "15",
            "--checks", "5",
            "--dry-run",
            "--verbose"
        };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.VerificationDelaySeconds.Should().Be(90);
        result.Config.PollingIntervalSeconds.Should().Be(15);
        result.Config.RequiredNoActivityChecks.Should().Be(5);
        result.Config.DryRun.Should().BeTrue();
        result.Config.Verbose.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithSteamAndEpicPaths_StoresBoth()
    {
        var args = new[] {
            "--steam-path", "/custom/steam",
            "--epic-path", "/custom/epic"
        };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.CustomSteamPath.Should().Be("/custom/steam");
        result.Config.CustomEpicPath.Should().Be("/custom/epic");
    }

    [Fact]
    public void ParseArguments_WithDefaultValues_ReturnsDefaults()
    {
        var args = Array.Empty<string>();
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeTrue();
        result.Config.VerificationDelaySeconds.Should().Be(60);
        result.Config.PollingIntervalSeconds.Should().Be(10);
        result.Config.RequiredNoActivityChecks.Should().Be(3);
        result.Config.MonitorSteam.Should().BeTrue();
        result.Config.MonitorEpic.Should().BeTrue();
        result.Config.CustomSteamPath.Should().BeNull();
        result.Config.CustomEpicPath.Should().BeNull();
        result.Config.DryRun.Should().BeFalse();
        result.Config.Verbose.Should().BeFalse();
    }

    #endregion

    #region Invalid Argument Tests

    [Theory]
    [InlineData(new[] { "--delay", "abc" }, "delay", "'abc' is not a valid number")]
    [InlineData(new[] { "--delay", "-5" }, "delay", "must be greater than 0, got -5")]
    [InlineData(new[] { "--delay", "0" }, "delay", "must be greater than 0, got 0")]
    [InlineData(new[] { "-d", "-1" }, "delay", "must be greater than 0, got -1")]
    [InlineData(new[] { "--interval", "abc" }, "interval", "'abc' is not a valid number")]
    [InlineData(new[] { "--interval", "-5" }, "interval", "must be greater than 0, got -5")]
    [InlineData(new[] { "--interval", "0" }, "interval", "must be greater than 0, got 0")]
    [InlineData(new[] { "-i", "-1" }, "interval", "must be greater than 0, got -1")]
    [InlineData(new[] { "--checks", "abc" }, "checks", "'abc' is not a valid number")]
    [InlineData(new[] { "--checks", "-5" }, "checks", "must be greater than 0, got -5")]
    [InlineData(new[] { "--checks", "0" }, "checks", "must be greater than 0, got 0")]
    [InlineData(new[] { "-c", "-1" }, "checks", "must be greater than 0, got -1")]
    public void ParseArguments_WithInvalidNumericValue_ReturnsError(string[] args, string option, string expectedError)
    {
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(option);
        result.ErrorMessage.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(new[] { "--delay" }, "delay", "requires a value")]
    [InlineData(new[] { "-d" }, "delay", "requires a value")]
    [InlineData(new[] { "--interval" }, "interval", "requires a value")]
    [InlineData(new[] { "-i" }, "interval", "requires a value")]
    [InlineData(new[] { "--checks" }, "checks", "requires a value")]
    [InlineData(new[] { "-c" }, "checks", "requires a value")]
    [InlineData(new[] { "--steam-path" }, "steam-path", "requires a value")]
    [InlineData(new[] { "--epic-path" }, "epic-path", "requires a value")]
    [InlineData(new[] { "--config" }, "config", "requires a value")]
    public void ParseArguments_WithMissingValue_ReturnsError(string[] args, string option, string expectedError)
    {
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(option);
        result.ErrorMessage.Should().Contain(expectedError);
    }

    [Fact]
    public void ParseArguments_WithUnknownOption_ReturnsError()
    {
        var args = new[] { "--unknown" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unknown option");
    }

    [Fact]
    public void ParseArguments_WithUnknownShortOption_ReturnsError()
    {
        var args = new[] { "-x" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unknown option");
    }

    [Fact]
    public void ParseArguments_WithInvalidLongOption_ReturnsError()
    {
        var args = new[] { "--invalid-option" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unknown option");
    }

    [Fact]
    public void ParseArguments_WithNonexistentConfigFile_ReturnsError()
    {
        var args = new[] { "--config", "/nonexistent/path/config.json" };
        var result = ApplyArguments(args);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("file not found");
    }

    #endregion

    #region Help and Version Tests

    [Fact]
    public void ParseArguments_WithHelp_ShowsHelpAndExits()
    {
        var args = new[] { "--help" };
        var result = ApplyArguments(args);

        result.ShouldExit.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ParseArguments_WithHelpShort_ShowsHelpAndExits()
    {
        var args = new[] { "-h" };
        var result = ApplyArguments(args);

        result.ShouldExit.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Environment Variable Tests

    [Fact]
    public void ParseArguments_WithEnvVarDelay_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY", "120");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.VerificationDelaySeconds.Should().Be(120);
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY", null);
        }
    }

    [Fact]
    public void ParseArguments_WithEnvVarInterval_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_POLLINGINTERVAL");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_POLLINGINTERVAL", "30");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.PollingIntervalSeconds.Should().Be(30);
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_POLLINGINTERVAL", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_POLLINGINTERVAL", null);
        }
    }

    [Fact]
    public void ParseArguments_WithEnvVarChecks_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_REQUIREDCHECKS");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_REQUIREDCHECKS", "7");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.RequiredNoActivityChecks.Should().Be(7);
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_REQUIREDCHECKS", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_REQUIREDCHECKS", null);
        }
    }

    [Fact]
    public void ParseArguments_WithEnvVarDryRun_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_DRYRUN");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_DRYRUN", "true");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.DryRun.Should().BeTrue();
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_DRYRUN", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_DRYRUN", null);
        }
    }

    [Fact]
    public void ParseArguments_WithEnvVarVerbose_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_VERBOSE");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_VERBOSE", "true");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.Verbose.Should().BeTrue();
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_VERBOSE", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_VERBOSE", null);
        }
    }

    [Fact]
    public void ParseArguments_WithEnvVarSteamPath_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_STEAMPATH");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_STEAMPATH", "/env/steam");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.CustomSteamPath.Should().Be("/env/steam");
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_STEAMPATH", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_STEAMPATH", null);
        }
    }

    [Fact]
    public void ParseArguments_WithEnvVarEpicPath_AppliesValue()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_EPICPATH");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_EPICPATH", "/env/epic");
            var args = Array.Empty<string>();
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.CustomEpicPath.Should().Be("/env/epic");
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_EPICPATH", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_EPICPATH", null);
        }
    }

    #endregion

    #region Config File Tests

    [Fact]
    public void ParseArguments_WithValidConfigFile_AppliesSettings()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"{
                ""PowerDown"": {
                    ""VerificationDelaySeconds"": 90,
                    ""PollingIntervalSeconds"": 15,
                    ""RequiredNoActivityChecks"": 5,
                    ""DryRun"": true,
                    ""Verbose"": true
                }
            }");
            var args = new[] { "--config", tempFile };
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.VerificationDelaySeconds.Should().Be(90);
            result.Config.PollingIntervalSeconds.Should().Be(15);
            result.Config.RequiredNoActivityChecks.Should().Be(5);
            result.Config.DryRun.Should().BeTrue();
            result.Config.Verbose.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseArguments_WithInvalidJsonConfig_ReturnsError()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"{
                ""PowerDown"": {
                    ""VerificationDelaySeconds"": invalid
                }
            }");
            var args = new[] { "--config", tempFile };
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("failed to parse");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Command Line Overrides Environment Tests

    [Fact]
    public void ParseArguments_CommandLineOverridesEnvVarDelay()
    {
        var originalValue = Environment.GetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY");
        try
        {
            Environment.SetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY", "100");
            var args = new[] { "--delay", "200" };
            var result = ApplyArguments(args);

            result.IsSuccess.Should().BeTrue();
            result.Config.VerificationDelaySeconds.Should().Be(200);
        }
        finally
        {
            if (originalValue != null)
                Environment.SetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY", originalValue);
            else
                Environment.SetEnvironmentVariable("POWERDOWN_VERIFICATIONDELAY", null);
        }
    }

    #endregion

    #region Helper Methods

    private static TestParseResult ApplyArguments(string[] args)
    {
        var programType = typeof(Program);

        var loadConfigMethod = programType.GetMethod("LoadConfiguration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var applyArgsMethod = programType.GetMethod("ApplyCommandLineArguments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (loadConfigMethod == null)
        {
            return new TestParseResult
            {
                IsSuccess = false,
                ErrorMessage = "LoadConfiguration method not found"
            };
        }

        if (applyArgsMethod == null)
        {
            return new TestParseResult
            {
                IsSuccess = false,
                ErrorMessage = "ApplyCommandLineArguments method not found"
            };
        }

        var emptyConfig = new ConfigurationBuilder().Build();
        var config = (Configuration)loadConfigMethod.Invoke(null, new object[] { emptyConfig })!;
        var result = (ParseResult)applyArgsMethod.Invoke(null, new object[] { config, args })!;

        return new TestParseResult
        {
            Config = config,
            IsSuccess = result.IsSuccess,
            ShouldExit = result.ShouldExit,
            ErrorMessage = result.ErrorMessage
        };
    }

    private class TestParseResult
    {
        public Configuration Config { get; set; } = new();
        public bool IsSuccess { get; set; }
        public bool ShouldExit { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}
