using Xunit;
using FluentAssertions;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Platform.Windows.Tests.Services;

public class EpicPathDetectorTests
{
    [Fact]
    public void DetectEpicPath_WithCustomPath_ReturnsCustomPath()
    {
        var customPath = @"C:\CustomEpic";
        var result = EpicPathDetector.DetectEpicPath(customPath);
        
        result.Should().Be(customPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectEpicPath_WithInvalidCustomPath_DoesNotReturnCustomPath(string? invalidPath)
    {
        var result = EpicPathDetector.DetectEpicPath(invalidPath);
        
        result.Should().NotBe(invalidPath);
    }

    [Fact]
    public void DetectEpicPath_WithNonExistentCustomPath_DoesNotReturnPath()
    {
        var nonExistentPath = @"Z:\NonExistent\Epic";
        var result = EpicPathDetector.DetectEpicPath(nonExistentPath);
        
        result.Should().BeNull();
    }

    [Fact]
    public void DetectEpicPath_WithNoCustomPath_DoesNotThrow()
    {
        Action act = () => EpicPathDetector.DetectEpicPath(null);
        
        act.Should().NotThrow();
    }
}
