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
    public void GetActiveDownloadsAsync_ReturnsEmptyListInitially()
    {
        var detector = new MockDownloadDetector();
        var downloads = detector.GetActiveDownloadsAsync().Result;
        downloads.Should().BeEmpty();
    }

    [Fact]
    public void IsAnyDownloadOrInstallActiveAsync_ReturnsFalseInitially()
    {
        var detector = new MockDownloadDetector();
        var result = detector.IsAnyDownloadOrInstallActiveAsync().Result;
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAnyDownloadOrInstallActiveAsync_ReturnsTrueWhenActive()
    {
        var detector = new MockDownloadDetector(isActive: true);
        var result = detector.IsAnyDownloadOrInstallActiveAsync().Result;
        result.Should().BeTrue();
    }

    [Fact]
    public void InitializeAsync_SetsInitializedToTrue()
    {
        var detector = new MockDownloadDetector();
        var result = detector.InitializeAsync().Result;
        result.Should().BeTrue();
        detector.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void SetActive_ChangesActiveState()
    {
        var detector = new MockDownloadDetector(isActive: false);
        detector.IsAnyDownloadOrInstallActiveAsync().Result.Should().BeFalse();

        detector.SetActive(true);
        detector.IsAnyDownloadOrInstallActiveAsync().Result.Should().BeTrue();
    }

    [Fact]
    public void SetDownloads_ReplacesDownloadList()
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
        var result = detector.GetActiveDownloadsAsync().Result;
        
        result.Should().HaveCount(1);
        result.Should().Contain(d => d.GameName == "Test Game");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_WithIsActiveParameter_SetsInitialState(bool isActive)
    {
        var detector = new MockDownloadDetector(isActive);
        detector.IsAnyDownloadOrInstallActiveAsync().Result.Should().Be(isActive);
    }
}
