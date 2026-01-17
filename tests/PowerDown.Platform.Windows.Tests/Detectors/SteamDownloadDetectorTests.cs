// Tests Steam detector behavior on Windows paths.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Platform.Windows.Detectors;
using Xunit;

namespace PowerDown.Platform.Windows.Tests.Detectors;

public class SteamDownloadDetectorTests : IDisposable
{
    private readonly Mock<ConsoleLogger> _mockLogger;
    private readonly string _tempSteamPath;

    public SteamDownloadDetectorTests()
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
        catch
        {
        }
    }

    [Fact]
    public void Constructor_WithValidPath_CreatesDetector()
    {
        var detector = new SteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        detector.Should().NotBeNull();
        detector.LauncherName.Should().Be("Steam");
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsException()
    {
        Action act = () => new SteamDownloadDetector(null!, _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Steam path is not configured");
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

        var detector = new SteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        var result = await detector.GetActiveDownloadsAsync(CancellationToken.None);
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.DownloadStatus == DownloadStatus.Downloading);
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

        var detector = new SteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        var result = await detector.GetActiveDownloadsAsync(CancellationToken.None);
        result.Should().Contain(d => d.GameName == "Test Game");
        result.Should().Contain(d => d.DownloadStatus == DownloadStatus.Downloading);
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

        var detector = new SteamDownloadDetector(_tempSteamPath, _mockLogger.Object);

        var result = await detector.GetActiveDownloadsAsync(CancellationToken.None);

        result.Should().Contain(d => d.GameName == "Library Game");
    }
}
