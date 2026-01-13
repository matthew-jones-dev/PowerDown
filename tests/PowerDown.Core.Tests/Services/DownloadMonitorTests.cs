using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Tests.Helpers;
using PowerDown.Core.Services;

namespace PowerDown.Core.Tests.Services;

public class DownloadMonitorTests
{
    private readonly MockDownloadDetector _mockDetector;
    private readonly Configuration _config;
    private readonly ConsoleLogger _logger;

    public DownloadMonitorTests()
    {
        _mockDetector = new MockDownloadDetector(new List<GameDownloadInfo>(), false);
        _config = new Configuration();
        _logger = new ConsoleLogger();
    }

    [Fact]
    public void Constructor_WithNullDetectors_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadMonitor(
            null!,
            _logger,
            _config,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("detectors");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadMonitor(
            new[] { _mockDetector },
            null!,
            _config,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            null!,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("config");
    }

    [Fact]
    public async Task InitializeDetectorsAsync_CallsInitializeOnAllDetectors()
    {
        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        await monitor.InitializeDetectorsAsync();

        _mockDetector.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_ReturnsDownloadsFromDetectors()
    {
        var downloads = new List<GameDownloadInfo>
        {
            new GameDownloadInfo
            {
                GameName = "Test Game",
                LauncherName = "Test Launcher",
                DownloadStatus = DownloadStatus.Downloading,
                Progress = 50.0
            }
        };
        _mockDetector.SetDownloads(downloads);

        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var result = await monitor.GetActiveDownloadsAsync();

        result.Should().HaveCount(1);
        result.Should().Contain(d => d.GameName == "Test Game");
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_WithNoActiveDetectors_ReturnsEmptyList()
    {
        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var result = await monitor.GetActiveDownloadsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IsAnyDownloadActiveAsync_WithActiveDownload_ReturnsTrue()
    {
        _mockDetector.SetActive(true);

        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var result = await monitor.IsAnyDownloadActiveAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAnyDownloadActiveAsync_WithNoActiveDownloads_ReturnsFalse()
    {
        _mockDetector.SetActive(false);

        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var result = await monitor.IsAnyDownloadActiveAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public void DisplayActiveDownloads_WithNoDownloads_DoesNotThrow()
    {
        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        // Should not throw
        monitor.DisplayActiveDownloads(new List<GameDownloadInfo>());
    }

    [Fact]
    public void DisplayActiveDownloads_WithDownloads_DoesNotThrow()
    {
        var monitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var downloads = new List<GameDownloadInfo>
        {
            new GameDownloadInfo
            {
                GameName = "Test Game",
                LauncherName = "Test Launcher",
                DownloadStatus = DownloadStatus.Downloading,
                Progress = 75.0
            }
        };

        // Should not throw
        monitor.DisplayActiveDownloads(downloads);
    }
}
