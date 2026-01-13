using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Core;
using PowerDown.Core.Detectors;
using PowerDown.Platform.Linux.Detectors;
using PowerDown.Platform.Linux.Services;
using Xunit;
using FluentAssertions;
using Moq;

namespace PowerDown.Platform.Linux.Tests;

public class LinuxSteamDownloadDetectorTests
{
    private readonly Mock<ConsoleLogger> _mockLogger;
    private readonly string _tempSteamPath;

    public LinuxSteamDownloadDetectorTests()
    {
        _mockLogger = new Mock<ConsoleLogger>();
        _tempSteamPath = Path.Combine(Path.GetTempPath(), $"steam_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempSteamPath);
        Directory.CreateDirectory(Path.Combine(_tempSteamPath, "logs"));
        Directory.CreateDirectory(Path.Combine(_tempSteamPath, "steamapps", "downloading"));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempSteamPath, true);
        }
        catch { }
    }

    [Fact]
    public void Constructor_WithValidPath_CreatesDetector()
    {
        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        detector.Should().NotBeNull();
        detector.LauncherName.Should().Be("Steam");
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsException()
    {
        Action act = () => new LinuxSteamDownloadDetector(null!, _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Steam path is not configured");
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsException()
    {
        Action act = () => new LinuxSteamDownloadDetector("", _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Steam path is not configured");
    }

    [Fact]
    public void InitializeAsync_WithNonexistentSteamPath_ThrowsException()
    {
        var detector = new LinuxSteamDownloadDetector("/nonexistent/steam", _mockLogger.Object);

        Func<Task> act = () => detector.InitializeAsync(CancellationToken.None);

        act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public void InitializeAsync_WithValidPath_ReturnsTrue()
    {
        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<bool>> act = () => detector.InitializeAsync(CancellationToken.None);

        act().Result.Should().BeTrue();
    }

    [Fact]
    public void GetActiveDownloadsAsync_WithNoDownloads_ReturnsEmpty()
    {
        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        act().Result.Should().BeEmpty();
    }

    [Fact]
    public void GetActiveDownloadsAsync_WithManifestFile_ReturnsGameInfo()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""name""		""Test Game""
	""StateFlags""		""1026""
}
");

        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        var result = act().Result;
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.DownloadStatus == DownloadStatus.Downloading);
    }

    [Fact]
    public void GetActiveDownloadsAsync_WithInstalledGame_RemovesFromList()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""name""		""Test Game""
	""StateFlags""		""4""
}
");

        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        act().Result.Should().NotContain(d => d.GameName == "Test Game");
    }

    [Fact]
    public void IsAnyDownloadOrInstallActiveAsync_WithDownloads_ReturnsTrue()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""name""		""Test Game""
	""StateFlags""		""1026""
}
");

        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<bool>> act = () => detector.IsAnyDownloadOrInstallActiveAsync(CancellationToken.None);

        act().Result.Should().BeTrue();
    }

    [Fact]
    public void IsAnyDownloadOrInstallActiveAsync_WithNoDownloads_ReturnsFalse()
    {
        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<bool>> act = () => detector.IsAnyDownloadOrInstallActiveAsync(CancellationToken.None);

        act().Result.Should().BeFalse();
    }

    [Fact]
    public void GetActiveDownloadsAsync_WithContentLog_ParsesDownload()
    {
        var logPath = Path.Combine(_tempSteamPath, "logs", "content_log.txt");
        File.WriteAllText(logPath, @"Downloading 1.5 GiB for Test Game - Game starting update");

        var detector = new LinuxSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        var result = act().Result;
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.DownloadStatus == DownloadStatus.Downloading);
    }
}

public class LinuxShutdownServiceTests
{
    [Fact]
    public void Constructor_CreatesService()
    {
        var service = new LinuxShutdownService();

        service.Should().NotBeNull();
    }

    [Fact]
    public void ScheduleShutdownAsync_WithZeroDelay_ThrowsException()
    {
        var service = new LinuxShutdownService();

        Func<Task> act = () => service.ScheduleShutdownAsync(0, "test message");

        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Delay must be greater than 0");
    }

    [Fact]
    public void ScheduleShutdownAsync_WithNegativeDelay_ThrowsException()
    {
        var service = new LinuxShutdownService();

        Func<Task> act = () => service.ScheduleShutdownAsync(-5, "test message");

        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Delay must be greater than 0");
    }
}
