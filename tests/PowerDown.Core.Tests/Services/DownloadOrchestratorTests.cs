using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Services;
using PowerDown.Core.Tests.Helpers;

namespace PowerDown.Core.Tests.Services;

public class DownloadOrchestratorTests : IDisposable
{
    private readonly Mock<IShutdownService> _shutdownServiceMock;
    private readonly Mock<ConsoleLogger> _loggerMock;
    private readonly MockDownloadDetector _mockDetector;

    public DownloadOrchestratorTests()
    {
        _shutdownServiceMock = new Mock<IShutdownService>();
        _loggerMock = new Mock<ConsoleLogger>();
        _mockDetector = new MockDownloadDetector(new List<GameDownloadInfo>(), false);
    }

    [Fact]
    public void Constructor_WithNullDetectors_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadOrchestrator(
            null!,
            _shutdownServiceMock.Object,
            _loggerMock.Object,
            new Configuration());

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("detectors");
    }

    [Fact]
    public void Constructor_WithNullShutdownService_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadOrchestrator(
            new List<IDownloadDetector>(),
            null!,
            _loggerMock.Object,
            new Configuration());

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("shutdownService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadOrchestrator(
            new List<IDownloadDetector>(),
            _shutdownServiceMock.Object,
            null!,
            new Configuration());

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Action act = () => new DownloadOrchestrator(
            new List<IDownloadDetector>(),
            _shutdownServiceMock.Object,
            _loggerMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("config");
    }

    [Fact]
    public async Task MonitorAndShutdownAsync_InitializesAllDetectors()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var config = new Configuration { DryRun = true };
        var orchestrator = new DownloadOrchestrator(
            new[] { _mockDetector },
            _shutdownServiceMock.Object,
            _loggerMock.Object,
            config);

        try
        {
            await orchestrator.MonitorAndShutdownAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        _mockDetector.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task MonitorAndShutdownAsync_WithDryRun_DoesNotCallShutdown()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var config = new Configuration { DryRun = true };
        var orchestrator = new DownloadOrchestrator(
            new[] { _mockDetector },
            _shutdownServiceMock.Object,
            _loggerMock.Object,
            config);

        _mockDetector.SetActive(false);
        try
        {
            await orchestrator.MonitorAndShutdownAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        _shutdownServiceMock.Verify(s => s.ScheduleShutdownAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    public void Dispose()
    {
        _shutdownServiceMock.VerifyNoOtherCalls();
    }
}
