using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;

namespace PowerDown.Core.Services;

public class DownloadOrchestrator : IDisposable
{
    private readonly IEnumerable<IDownloadDetector> _detectors;
    private readonly IShutdownService _shutdownService;
    private readonly ConsoleLogger _logger;
    private readonly Configuration _config;
    private readonly CancellationTokenSource _cts;

    public DownloadOrchestrator(
        IEnumerable<IDownloadDetector> detectors,
        IShutdownService shutdownService,
        ConsoleLogger logger,
        Configuration config)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _shutdownService = shutdownService ?? throw new ArgumentNullException(nameof(shutdownService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cts = new CancellationTokenSource();

        Console.CancelKeyPress += OnCancelKeyPress;
    }

    public async Task MonitorAndShutdownAsync(CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

        _logger.LogInfo($"PowerDown v1.0.0 starting...");
        _logger.LogInfo($"Verification delay: {_config.VerificationDelaySeconds}s, Polling interval: {_config.PollingIntervalSeconds}s, Required checks: {_config.RequiredNoActivityChecks}");
        _logger.LogInfo($"Monitoring Steam: {_config.MonitorSteam}");
        _logger.LogInfo($"Monitoring Epic Games: {_config.MonitorEpic}");
        _logger.LogInfo($"Dry run mode: {_config.DryRun}");

        var downloadMonitor = new DownloadMonitor(_detectors, _logger, _config, token);
        var verificationEngine = new VerificationEngine(downloadMonitor, _logger, _config, token);
        var shutdownScheduler = new ShutdownScheduler(_shutdownService, _logger, _config, token);

        await downloadMonitor.InitializeDetectorsAsync();
        
        var initialDownloads = await downloadMonitor.GetActiveDownloadsAsync();
        downloadMonitor.DisplayActiveDownloads(initialDownloads);

        if (!initialDownloads.Any())
        {
            _logger.LogInfo("No downloads in progress - Waiting for downloads to start...");
            await downloadMonitor.WaitForDownloadsToStartAsync();
        }

        _logger.LogInfo("Monitoring downloads...");
        await downloadMonitor.WaitForAllDownloadsToCompleteAsync();

        _logger.LogSuccess("All downloads complete - Starting verification period...");
        shutdownScheduler.SetVerificationPeriod(true);
        await verificationEngine.RunVerificationPollingAsync();

        await shutdownScheduler.ScheduleShutdownAsync();
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger.LogInfo("Cancellation requested...");
        
        var downloadMonitor = new DownloadMonitor(_detectors, _logger, _config, _cts.Token);
        var shutdownScheduler = new ShutdownScheduler(_shutdownService, _logger, _config, _cts.Token);
        
        _ = shutdownScheduler.CancelShutdownIfNeededAsync();
        
        _cts.Cancel();
        e.Cancel = true;
    }

    public void Dispose()
    {
        _cts.Dispose();
        Console.CancelKeyPress -= OnCancelKeyPress;
    }
}
