using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Detectors;
using PowerDown.Platform.macOS.Detectors;
using PowerDown.Platform.macOS.Services;
using Xunit;
using FluentAssertions;
using Moq;

namespace PowerDown.Platform.macOS.Tests;

public class MacSteamDownloadDetectorTests : IDisposable
{
    private readonly Mock<ConsoleLogger> _mockLogger;
    private readonly string _tempSteamPath;

    public MacSteamDownloadDetectorTests()
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
        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        detector.Should().NotBeNull();
        detector.LauncherName.Should().Be("Steam");
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsException()
    {
        Action act = () => new MacSteamDownloadDetector(null!, _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Steam path is not configured");
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsException()
    {
        Action act = () => new MacSteamDownloadDetector("", _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Steam path is not configured");
    }

    [Fact]
    public void InitializeAsync_WithNonexistentSteamPath_ThrowsException()
    {
        var detector = new MacSteamDownloadDetector("/nonexistent/steam", _mockLogger.Object);

        Func<Task> act = () => detector.InitializeAsync(CancellationToken.None);

        act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task InitializeAsync_WithValidPath_ReturnsTrue()
    {
        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<bool>> act = () => detector.InitializeAsync(CancellationToken.None);

        (await act()).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithNoDownloads_ReturnsEmpty()
    {
        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        (await act()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithManifestFile_ReturnsGameInfo()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""appid""		""12345""
	""name""		""Test Game""
	""StateFlags""		""1026""
	""BytesToDownload""		""100""
	""BytesDownloaded""		""50""
	""BytesToStage""		""0""
	""BytesStaged""		""0""
}
");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        var result = await act();
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.DownloadStatus == DownloadStatus.Downloading);
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithStagingBytes_ReturnsInstalling()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""appid""		""12345""
	""name""		""Test Game""
	""StateFlags""		""4""
	""BytesToDownload""		""0""
	""BytesDownloaded""		""0""
	""BytesToStage""		""100""
	""BytesStaged""		""50""
}
");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        var result = await detector.GetActiveDownloadsAsync(CancellationToken.None);

        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.InstallStatus == DownloadStatus.Installing);
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithLibraryFolderManifest_ReturnsGameInfo()
    {
        var libraryPath = Path.Combine(_tempSteamPath, "library1");
        var librarySteamApps = Path.Combine(libraryPath, "steamapps");
        Directory.CreateDirectory(librarySteamApps);

        var libraryFile = Path.Combine(_tempSteamPath, "steamapps", "libraryfolders.vdf");
        File.WriteAllText(libraryFile, $@"
""libraryfolders""
{{
	""0""
	{{
		""path""		""{libraryPath}""
	}}
}}
");

        var manifestPath = Path.Combine(librarySteamApps, "appmanifest_98765.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""appid""		""98765""
	""name""		""Library Game""
	""StateFlags""		""1026""
	""BytesToDownload""		""100""
	""BytesDownloaded""		""10""
	""BytesToStage""		""0""
	""BytesStaged""		""0""
}
");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        var result = await detector.GetActiveDownloadsAsync(CancellationToken.None);

        result.Should().Contain(d => d.GameName == "Library Game");
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithInstalledGame_RemovesFromList()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""name""		""Test Game""
	""StateFlags""		""4""
	""BytesToDownload""		""100""
	""BytesDownloaded""		""100""
	""BytesToStage""		""0""
	""BytesStaged""		""0""
}
");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        (await act()).Should().NotContain(d => d.GameName == "Test Game");
    }

    [Fact]
    public async Task IsAnyDownloadOrInstallActiveAsync_WithDownloads_ReturnsTrue()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""name""		""Test Game""
	""StateFlags""		""1026""
	""BytesToDownload""		""100""
	""BytesDownloaded""		""50""
	""BytesToStage""		""0""
	""BytesStaged""		""0""
}
");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<bool>> act = () => detector.IsAnyDownloadOrInstallActiveAsync(CancellationToken.None);

        (await act()).Should().BeTrue();
    }

    [Fact]
    public async Task IsAnyDownloadOrInstallActiveAsync_WithNoDownloads_ReturnsFalse()
    {
        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<bool>> act = () => detector.IsAnyDownloadOrInstallActiveAsync(CancellationToken.None);

        (await act()).Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithContentLog_ParsesDownload()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""appid""		""12345""
	""name""		""Test Game""
	""StateFlags""		""1026""
}
");

        var logPath = Path.Combine(_tempSteamPath, "logs", "content_log.txt");
        File.WriteAllText(logPath, @"AppID 12345 update changed : Running Update,Downloading,Staging,");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        Func<Task<IEnumerable<GameDownloadInfo>>> act = () => 
            detector.GetActiveDownloadsAsync(CancellationToken.None);

        var result = await act();
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.DownloadStatus == DownloadStatus.Downloading);
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithContentLogInstalling_ParsesInstalling()
    {
        var manifestPath = Path.Combine(_tempSteamPath, "steamapps", "appmanifest_12345.acf");
        File.WriteAllText(manifestPath, @"
""appinfo""
{
	""appid""		""12345""
	""name""		""Test Game""
	""StateFlags""		""6""
}
");

        var logPath = Path.Combine(_tempSteamPath, "logs", "content_log.txt");
        File.WriteAllText(logPath, @"AppID 12345 update changed : Running Update,Committing,");

        var detector = new MacSteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        var result = await detector.GetActiveDownloadsAsync(CancellationToken.None);
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.InstallStatus == DownloadStatus.Installing);
    }
}

public class MacShutdownServiceTests
{
    [Fact]
    public void Constructor_CreatesService()
    {
        var service = new MacShutdownService();

        service.Should().NotBeNull();
    }

    [Fact]
    public void ScheduleShutdownAsync_WithZeroDelay_ThrowsException()
    {
        var service = new MacShutdownService();

        Func<Task> act = () => service.ScheduleShutdownAsync(0, "test message");

        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Delay must be greater than 0");
    }

    [Fact]
    public void ScheduleShutdownAsync_WithNegativeDelay_ThrowsException()
    {
        var service = new MacShutdownService();

        Func<Task> act = () => service.ScheduleShutdownAsync(-5, "test message");

        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Delay must be greater than 0");
    }
}
