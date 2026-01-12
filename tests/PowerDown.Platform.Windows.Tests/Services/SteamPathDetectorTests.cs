using System.IO;
using Xunit;
using FluentAssertions;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Platform.Windows.Tests.Services;

public class SteamPathDetectorTests
{
    [Fact]
    public void DetectSteamPath_WithCustomPath_ReturnsCustomPath()
    {
        var customPath = @"C:\CustomSteam";
        var result = SteamPathDetector.DetectSteamPath(customPath);
        
        result.Should().Be(customPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectSteamPath_WithInvalidCustomPath_DoesNotReturnCustomPath(string? invalidPath)
    {
        var result = SteamPathDetector.DetectSteamPath(invalidPath);
        
        result.Should().NotBe(invalidPath);
    }

    [Fact]
    public void DetectSteamPath_WithNonExistentCustomPath_DoesNotReturnPath()
    {
        var nonExistentPath = @"Z:\NonExistent\Steam";
        var result = SteamPathDetector.DetectSteamPath(nonExistentPath);
        
        result.Should().BeNull();
    }

    [Fact]
    public void DetectSteamPath_WithNoCustomPath_DoesNotThrow()
    {
        Action act = () => SteamPathDetector.DetectSteamPath(null);
        
        act.Should().NotThrow();
    }
}
