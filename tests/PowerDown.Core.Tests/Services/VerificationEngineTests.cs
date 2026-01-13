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

public class VerificationEngineTests
{
    private readonly MockDownloadDetector _mockDetector;
    private readonly Configuration _config;
    private readonly ConsoleLogger _logger;

    public VerificationEngineTests()
    {
        _mockDetector = new MockDownloadDetector(new List<GameDownloadInfo>(), false);
        _config = new Configuration
        {
            VerificationDelaySeconds = 1,
            PollingIntervalSeconds = 50,
            RequiredNoActivityChecks = 1
        };
        _logger = new ConsoleLogger();
    }

    [Fact]
    public void Constructor_WithNullDownloadMonitor_ThrowsArgumentNullException()
    {
        Action act = () => new VerificationEngine(
            null!,
            _logger,
            _config,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("downloadMonitor");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var downloadMonitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        Action act = () => new VerificationEngine(
            downloadMonitor,
            null!,
            _config,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        var downloadMonitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        Action act = () => new VerificationEngine(
            downloadMonitor,
            _logger,
            null!,
            CancellationToken.None);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("config");
    }

    [Fact]
    public async Task RunVerificationPollingAsync_CompletesWhenChecksSatisfied()
    {
        var downloadMonitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var engine = new VerificationEngine(
            downloadMonitor,
            _logger,
            _config,
            CancellationToken.None);

        // Should complete without throwing
        await engine.RunVerificationPollingAsync();
        
        // Verify it logged success
        // (In real scenario, would verify logging, but here we just ensure no exception)
    }

    [Fact]
    public async Task RunVerificationPollingAsync_CancelledToken_ThrowsTaskCanceledException()
    {
        var downloadMonitor = new DownloadMonitor(
            new[] { _mockDetector },
            _logger,
            _config,
            CancellationToken.None);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(10); // Cancel almost immediately

        var engine = new VerificationEngine(
            downloadMonitor,
            _logger,
            _config,
            cts.Token);

        // Should throw TaskCanceledException
        Func<Task> act = async () => await engine.RunVerificationPollingAsync();
        await act.Should().ThrowAsync<TaskCanceledException>();
    }
}
