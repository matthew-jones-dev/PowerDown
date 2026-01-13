using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using PowerDown.Abstractions;
using PowerDown.Core;
using PowerDown.Core.Services;

namespace PowerDown.Core.Tests.Services;

public class ShutdownSchedulerTests
{
    private readonly Mock<IShutdownService> _shutdownServiceMock;
    private readonly ConsoleLogger _logger;
    private readonly Configuration _config;

    public ShutdownSchedulerTests()
    {
        _shutdownServiceMock = new Mock<IShutdownService>();
        _logger = new ConsoleLogger();
        _config = new Configuration();
    }

    [Fact]
    public void Constructor_WithNullShutdownService_ThrowsArgumentNullException()
    {
        Action act = () => new ShutdownScheduler(
            null!,
            _logger,
            _config,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("shutdownService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new ShutdownScheduler(
            _shutdownServiceMock.Object,
            null!,
            _config,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        Action act = () => new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            null!,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("config");
    }

    [Fact]
    public void IsVerificationPeriod_InitialValueIsFalse()
    {
        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            CancellationToken.None);

        scheduler.IsVerificationPeriod.Should().BeFalse();
    }

    [Fact]
    public void SetVerificationPeriod_UpdatesIsVerificationPeriod()
    {
        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            CancellationToken.None);

        scheduler.SetVerificationPeriod(true);
        scheduler.IsVerificationPeriod.Should().BeTrue();

        scheduler.SetVerificationPeriod(false);
        scheduler.IsVerificationPeriod.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleShutdownAsync_WithDryRun_DoesNotCallShutdownService()
    {
        _config.DryRun = true;

        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            CancellationToken.None);

        await scheduler.ScheduleShutdownAsync();

        _shutdownServiceMock.Verify(s => s.ScheduleShutdownAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleShutdownAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10); // Cancel almost immediately

        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            cts.Token);

        Func<Task> act = async () => await scheduler.ScheduleShutdownAsync();
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task CancelShutdownIfNeededAsync_WithNoVerificationPeriod_DoesNotCancel()
    {
        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            CancellationToken.None);

        await scheduler.CancelShutdownIfNeededAsync();

        _shutdownServiceMock.Verify(s => s.CancelShutdownAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelShutdownIfNeededAsync_WithDryRun_DoesNotCancel()
    {
        _config.DryRun = true;

        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            CancellationToken.None);

        scheduler.SetVerificationPeriod(true);
        await scheduler.CancelShutdownIfNeededAsync();

        _shutdownServiceMock.Verify(s => s.CancelShutdownAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelShutdownIfNeededAsync_WithVerificationPeriod_CancelsShutdown()
    {
        _shutdownServiceMock.Setup(s => s.CancelShutdownAsync())
            .Returns(Task.CompletedTask);

        var scheduler = new ShutdownScheduler(
            _shutdownServiceMock.Object,
            _logger,
            _config,
            CancellationToken.None);

        scheduler.SetVerificationPeriod(true);
        await scheduler.CancelShutdownIfNeededAsync();

        _shutdownServiceMock.Verify(s => s.CancelShutdownAsync(), Times.Once);
    }
}
