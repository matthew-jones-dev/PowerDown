using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Tests.Helpers;

namespace PowerDown.Platform.Windows.Tests.Detectors;

public class MockDownloadDetectorTests
{
    [Fact]
    public void LauncherName_ReturnsCorrectValue()
    {
        var detector = new MockDownloadDetector();
        detector.LauncherName.Should().Be("Mock Launcher");
    }

    [Fact]
    public async Task GetActiveDownloadsAsync_ReturnsEmptyListInitially()
    {
        var detector = new MockDownloadDetector();
        var downloads = await detector.GetActiveDownloadsAsync();
        downloads.Should().BeEmpty();
    }

    [Fact]
    public async Task IsAnyDownloadOrInstallActiveAsync_ReturnsFalseInitially()
    {
        var detector = new MockDownloadDetector();
        var result = await detector.IsAnyDownloadOrInstallActiveAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAnyDownloadOrInstallActiveAsync_ReturnsTrueWhenActive()
    {
        var detector = new MockDownloadDetector(isActive: true);
        var result = await detector.IsAnyDownloadOrInstallActiveAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_SetsInitializedToTrue()
    {
        var detector = new MockDownloadDetector();
        var result = await detector.InitializeAsync();
        result.Should().BeTrue();
        detector.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task SetActive_ChangesActiveState()
    {
        var detector = new MockDownloadDetector(isActive: false);
        (await detector.IsAnyDownloadOrInstallActiveAsync()).Should().BeFalse();

        detector.SetActive(true);
        (await detector.IsAnyDownloadOrInstallActiveAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SetDownloads_ReplacesDownloadList()
    {
        var detector = new MockDownloadDetector();
        
        var downloads = new List<GameDownloadInfo>
        {
            new GameDownloadInfo
            {
                GameName = "Test Game",
                LauncherName = "Test",
                DownloadStatus = DownloadStatus.Downloading,
                Progress = 50.0
            }
        };
        
        detector.SetDownloads(downloads);
        var result = await detector.GetActiveDownloadsAsync();
        
        result.Should().HaveCount(1);
        result.Should().Contain(d => d.GameName == "Test Game");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Constructor_WithIsActiveParameter_SetsInitialState(bool isActive)
    {
        var detector = new MockDownloadDetector(isActive);
        (await detector.IsAnyDownloadOrInstallActiveAsync()).Should().Be(isActive);
    }
}
