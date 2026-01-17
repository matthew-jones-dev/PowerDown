// Tests Steam path detection behavior on Linux.
using System;
using System.IO;
using FluentAssertions;
using Moq;
using PowerDown.Abstractions.Interfaces;
using PowerDown.Platform.Linux.Services;
using Xunit;

namespace PowerDown.Platform.Linux.Tests.Services;

public class LinuxSteamPathDetectorTests : IDisposable
{
    private readonly string _originalHome;
    private readonly string _tempHome;

    public LinuxSteamPathDetectorTests()
    {
        _originalHome = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;
        _tempHome = Path.Combine(Path.GetTempPath(), $"steam_home_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempHome);
        Environment.SetEnvironmentVariable("HOME", _tempHome);
    }

    [Fact]
    public void DetectSteamPath_WithCustomPath_ReturnsCustomPath()
    {
        var customPath = Path.Combine(_tempHome, "SteamCustom");
        Directory.CreateDirectory(customPath);

        var detector = new LinuxSteamPathDetector(new Mock<ILogger>().Object);

        detector.DetectSteamPath(customPath).Should().Be(customPath);
    }

    [Fact]
    public void DetectSteamPath_WithFlatpakInstall_ReturnsFlatpakPath()
    {
        var flatpakPath = Path.Combine(_tempHome, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
        Directory.CreateDirectory(flatpakPath);

        var detector = new LinuxSteamPathDetector(new Mock<ILogger>().Object);

        detector.DetectSteamPath(null).Should().Be(flatpakPath);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("HOME", _originalHome);
        try
        {
            Directory.Delete(_tempHome, true);
        }
        catch
        {
        }
    }
}
