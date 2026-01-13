using System;
using System.IO;
using Xunit;
using FluentAssertions;
using PowerDown.Core;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Platform.Windows.Tests.Services;

public class EpicPathDetectorTests
{
    private readonly EpicPathDetector _detector;
    private readonly ConsoleLogger _logger;

    public EpicPathDetectorTests()
    {
        _logger = new ConsoleLogger();
        _detector = new EpicPathDetector(_logger);
    }

    [Fact]
    public void DetectEpicPath_WithCustomPath_ReturnsCustomPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows - paths are platform-specific
        }
        
        var customPath = @"C:\CustomEpic";
        var result = _detector.DetectEpicPath(customPath);
        
        result.Should().Be(customPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectEpicPath_WithInvalidCustomPath_DoesNotReturnCustomPath(string? invalidPath)
    {
        var result = _detector.DetectEpicPath(invalidPath);
        
        // When custom path is null/empty/whitespace, function attempts auto-detection
        // On Windows this may succeed; on Linux it typically returns null
        // The key is that it should NOT throw and should not return the invalid path
        if (result != null)
        {
            result.Should().NotBe(invalidPath);
        }
    }

    [Fact]
    public void DetectEpicPath_WithNonExistentCustomPath_DoesNotReturnPath()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent", "Epic");
        var result = _detector.DetectEpicPath(nonExistentPath);
        
        result.Should().BeNull();
    }

    [Fact]
    public void DetectEpicPath_WithNoCustomPath_DoesNotThrow()
    {
        Action act = () => _detector.DetectEpicPath(null);
        
        act.Should().NotThrow();
    }
}
