using System;
using System.IO;
using Xunit;
using FluentAssertions;
using PowerDown.Core;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Platform.Windows.Tests.Services;

public class SteamPathDetectorTests
{
    private readonly SteamPathDetector _detector;
    private readonly ConsoleLogger _logger;

    public SteamPathDetectorTests()
    {
        _logger = new ConsoleLogger();
        _detector = new SteamPathDetector(_logger);
    }

    [Fact]
    public void DetectSteamPath_WithCustomPath_ReturnsCustomPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var customPath = Path.Combine(Path.GetTempPath(), "CustomSteam_" + Guid.NewGuid());
        Directory.CreateDirectory(customPath);
        try
        {
            var result = _detector.DetectSteamPath(customPath);
            result.Should().Be(customPath);
        }
        finally
        {
            Directory.Delete(customPath);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectSteamPath_WithInvalidCustomPath_DoesNotReturnCustomPath(string? invalidPath)
    {
        var result = _detector.DetectSteamPath(invalidPath);
        
        // When custom path is null/empty/whitespace, function attempts auto-detection
        // On Windows this may succeed; on Linux it typically returns null
        // The key is that it should NOT throw and should not return the invalid path
        if (result != null)
        {
            result.Should().NotBe(invalidPath);
        }
    }

    [Fact]
    public void DetectSteamPath_WithNonExistentCustomPath_DoesNotReturnPath()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent", "Steam");
        var result = _detector.DetectSteamPath(nonExistentPath);
        
        result.Should().BeNull();
    }

    [Fact]
    public void DetectSteamPath_WithNoCustomPath_DoesNotThrow()
    {
        Action act = () => _detector.DetectSteamPath(null);
        
        act.Should().NotThrow();
    }
}
