using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Core;

namespace PowerDown.Core.Services;

/// <summary>
/// Handles download monitoring and polling logic
/// </summary>
public class DownloadMonitor
{
    private readonly IEnumerable<IDownloadDetector> _detectors;
    private readonly ConsoleLogger _logger;
    private readonly Configuration _config;
    private readonly CancellationToken _cancellationToken;

    public DownloadMonitor(
        IEnumerable<IDownloadDetector> detectors,
        ConsoleLogger logger,
        Configuration config,
        CancellationToken cancellationToken)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationToken = cancellationToken;
    }

    public async Task InitializeDetectorsAsync()
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

    public async Task<List<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        _logger.LogVerbose("Getting initial download status...");
        
        var allDownloads = new List<GameDownloadInfo>();
        
        foreach (var detector in _detectors)
        {
            try
            {
                var downloads = (await detector.GetActiveDownloadsAsync(_cancellationToken)).ToList();
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

    public void DisplayActiveDownloads(IEnumerable<GameDownloadInfo> downloads)
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

    public async Task WaitForDownloadsToStartAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                var hasActivity = false;
                
                foreach (var detector in _detectors)
                {
                    if (await detector.IsAnyDownloadOrInstallActiveAsync(_cancellationToken))
                    {
                        hasActivity = true;
                        _logger.LogInfo($"Download detected on {detector.LauncherName}");
                        break;
                    }
                }

                if (hasActivity)
                {
                    return;
                }

                await Task.Delay(5000, _cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    public async Task WaitForAllDownloadsToCompleteAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                var allDownloads = await GetActiveDownloadsAsync();
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

                await Task.Delay(_config.PollingIntervalSeconds * 1000, _cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    public async Task<bool> IsAnyDownloadActiveAsync()
    {
        foreach (var detector in _detectors)
        {
            if (await detector.IsAnyDownloadOrInstallActiveAsync(_cancellationToken))
            {
                return true;
            }
        }
        return false;
    }
}
