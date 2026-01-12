using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;

namespace PowerDown.Core.Services;

public class DownloadOrchestrator
{
    private readonly IEnumerable<IDownloadDetector> _detectors;
    private readonly IShutdownService _shutdownService;
    private readonly ConsoleLogger _logger;
    private readonly Configuration _config;
    private readonly CancellationTokenSource _cts;

    private bool _hasSeenDownloads = false;
    private bool _isVerificationPeriod = false;

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

    public async Task MonitorAndShutdownAsync()
    {
        _logger.LogInfo($"PowerDown v1.0.0 starting...");
        _logger.LogInfo($"Verification delay: {_config.VerificationDelaySeconds}s, Polling interval: {_config.PollingIntervalSeconds}s, Required checks: {_config.RequiredNoActivityChecks}");
        _logger.LogInfo($"Monitoring Steam: {_config.MonitorSteam}");
        _logger.LogInfo($"Monitoring Epic Games: {_config.MonitorEpic}");
        _logger.LogInfo($"Dry run mode: {_config.DryRun}");

        await InitializeDetectorsAsync();
        
        var initialDownloads = await GetInitialDownloadStatusAsync();
        DisplayActiveDownloads(initialDownloads);

        if (!initialDownloads.Any())
        {
            _logger.LogInfo("No downloads in progress - Waiting for downloads to start...");
            await WaitForDownloadsToStartAsync();
        }

        _logger.LogInfo("Monitoring downloads...");
        await WaitForAllDownloadsToCompleteAsync();

        _logger.LogSuccess("All downloads complete - Starting verification period...");
        await RunVerificationPollingAsync();

        await ScheduleShutdownAsync();
    }

    private async Task InitializeDetectorsAsync()
    {
        _logger.LogVerbose("Initializing detectors...");
        
        foreach (var detector in _detectors)
        {
            try
            {
                var initialized = await detector.InitializeAsync();
                _logger.LogVerbose($"{detector.LauncherName} detector initialized: {initialized}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to initialize {detector.LauncherName} detector: {ex.Message}");
            }
        }
    }

    private async Task<List<GameDownloadInfo>> GetInitialDownloadStatusAsync()
    {
        _logger.LogVerbose("Getting initial download status...");
        
        var allDownloads = new List<GameDownloadInfo>();
        
        foreach (var detector in _detectors)
        {
            try
            {
                var downloads = (await detector.GetActiveDownloadsAsync()).ToList();
                allDownloads.AddRange(downloads);
                _logger.LogVerbose($"{detector.LauncherName}: {downloads.Count} active download(s)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting downloads from {detector.LauncherName}: {ex.Message}");
            }
        }
        
        return allDownloads;
    }

    private void DisplayActiveDownloads(IEnumerable<GameDownloadInfo> downloads)
    {
        var downloadList = downloads.ToList();
        
        if (!downloadList.Any())
        {
            _logger.LogInfo("No active downloads detected.");
            return;
        }

        _logger.LogInfo($"Active Downloads ({downloadList.Count}):");
        
        foreach (var download in downloadList)
        {
            var status = download.DownloadStatus == DownloadStatus.Downloading ? "Downloading" : "Installing";
            _logger.LogInfo($"  - {download.GameName} ({download.LauncherName}): {status} {download.Progress:F0}%");
        }
    }

    private async Task WaitForDownloadsToStartAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var hasActivity = false;
                
                foreach (var detector in _detectors)
                {
                    if (await detector.IsAnyDownloadOrInstallActiveAsync())
                    {
                        hasActivity = true;
                        _logger.LogInfo($"Download detected on {detector.LauncherName}");
                        break;
                    }
                }

                if (hasActivity)
                {
                    _hasSeenDownloads = true;
                    return;
                }

                await Task.Delay(5000, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task WaitForAllDownloadsToCompleteAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var allDownloads = await GetInitialDownloadStatusAsync();
                DisplayActiveDownloads(allDownloads);
                
                var hasAnyActive = false;
                
                foreach (var download in allDownloads)
                {
                    if (download.DownloadStatus == DownloadStatus.Downloading || 
                        download.InstallStatus == DownloadStatus.Installing)
                    {
                        hasAnyActive = true;
                        break;
                    }
                }

                if (!hasAnyActive)
                {
                    _logger.LogInfo("All downloads and installations complete!");
                    return;
                }

                await Task.Delay(_config.PollingIntervalSeconds * 1000, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task RunVerificationPollingAsync()
    {
        _isVerificationPeriod = true;
        var noActivityCount = 0;
        var elapsedTime = TimeSpan.Zero;
        var totalDelay = TimeSpan.FromSeconds(_config.VerificationDelaySeconds);
        var pollingInterval = TimeSpan.FromSeconds(_config.PollingIntervalSeconds);

        _logger.LogVerbose("Starting verification polling...");

        while (elapsedTime < totalDelay && !_cts.Token.IsCancellationRequested)
        {
            await Task.Delay(pollingInterval, _cts.Token);
            elapsedTime += pollingInterval;

            try
            {
                var hasAnyActive = false;
                
                foreach (var detector in _detectors)
                {
                    if (await detector.IsAnyDownloadOrInstallActiveAsync())
                    {
                        hasAnyActive = true;
                        break;
                    }
                }

                if (hasAnyActive)
                {
                    _logger.LogWarning($"Download resumed during verification - Resetting counter...");
                    noActivityCount = 0;
                    await WaitForAllDownloadsToCompleteAsync();
                }
                else
                {
                    noActivityCount++;
                    _logger.LogInfo($"Polling check {noActivityCount}/{_config.RequiredNoActivityChecks}: No activity detected");

                    if (noActivityCount >= _config.RequiredNoActivityChecks)
                    {
                        _logger.LogSuccess("Verification complete - No downloads resumed!");
                        return;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task ScheduleShutdownAsync()
    {
        if (_config.DryRun)
        {
            _logger.LogInfo("Dry run mode: Would shutdown now");
            return;
        }

        _logger.LogInfo("Initiating shutdown in 30 seconds...");
        _logger.LogInfo("Press CTRL+C to cancel");
        await Task.Delay(30000, _cts.Token);

        if (!_cts.Token.IsCancellationRequested)
        {
            await _shutdownService.ScheduleShutdownAsync(30, "PowerDown: All downloads complete");
            _logger.LogSuccess("Shutdown scheduled");
        }
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger.LogInfo("Cancellation requested...");
        
        if (_isVerificationPeriod && !_config.DryRun)
        {
            _logger.LogInfo("Cancelling scheduled shutdown...");
            _shutdownService.CancelShutdownAsync().Wait();
        }
        
        _cts.Cancel();
        e.Cancel = true;
    }

    public void Dispose()
    {
        _cts.Dispose();
        Console.CancelKeyPress -= OnCancelKeyPress;
    }
}
