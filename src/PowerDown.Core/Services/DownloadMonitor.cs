using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Core.Services;

public class DownloadMonitor
{
    private readonly IEnumerable<IDownloadDetector> _detectors;
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly CancellationToken _cancellationToken;
    private readonly IStatusNotifier _statusNotifier;
    private readonly Dictionary<string, GameDownloadInfo> _lastDownloads = new();

    public DownloadMonitor(
        IEnumerable<IDownloadDetector> detectors,
        ILogger logger,
        Configuration config,
        CancellationToken cancellationToken,
        IStatusNotifier? statusNotifier = null)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationToken = cancellationToken;
        _statusNotifier = statusNotifier ?? new StatusNotifier();
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
        var currentDownloads = new Dictionary<string, GameDownloadInfo>();
        
        foreach (var detector in _detectors)
        {
            try
            {
                var downloads = (await detector.GetActiveDownloadsAsync(_cancellationToken)).ToList();
                allDownloads.AddRange(downloads);
                _logger.LogVerbose($"{detector.LauncherName}: {downloads.Count} active download(s)");
                
                foreach (var download in downloads)
                {
                    var key = $"{download.LauncherName}|{download.GameName}";
                    currentDownloads[key] = download;
                    _statusNotifier.NotifyDownloadUpdate(new DownloadUpdate
                    {
                        GameName = download.GameName,
                        LauncherName = download.LauncherName,
                        Status = download.DownloadStatus,
                        Progress = download.Progress,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting downloads from {detector.LauncherName}: {ex.Message}");
            }
        }

        foreach (var previous in _lastDownloads.Values)
        {
            var key = $"{previous.LauncherName}|{previous.GameName}";
            if (!currentDownloads.ContainsKey(key))
            {
                _statusNotifier.NotifyDownloadUpdate(new DownloadUpdate
                {
                    GameName = previous.GameName,
                    LauncherName = previous.LauncherName,
                    Status = DownloadStatus.Idle,
                    Progress = 100,
                    Timestamp = DateTime.Now
                });
            }
        }

        _lastDownloads.Clear();
        foreach (var pair in currentDownloads)
        {
            _lastDownloads[pair.Key] = pair.Value;
        }
        
        return allDownloads;
    }

    public void DisplayActiveDownloads(IEnumerable<GameDownloadInfo> downloads)
    {
        var downloadList = downloads.ToList();
        
        if (!downloadList.Any())
        {
            var message = "No active downloads detected.";
            _logger.LogInfo(message);
            _statusNotifier.NotifyStatus(message);
            return;
        }

        var message2 = $"Active Downloads ({downloadList.Count}):";
        _logger.LogInfo(message2);
        _statusNotifier.NotifyStatus(message2);
        
        foreach (var download in downloadList)
        {
            var status = download.DownloadStatus == DownloadStatus.Downloading ? "Downloading" : "Installing";
            var percentSuffix = download.Progress > 0 && download.Progress < 100
                ? $" {download.Progress:F0}%"
                : string.Empty;
            var statusMessage = $"  - {download.GameName} ({download.LauncherName}): {status}{percentSuffix}";
            _logger.LogInfo(statusMessage);
            _statusNotifier.NotifyStatus(statusMessage);
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
                        var message = $"Download detected on {detector.LauncherName}";
                        _logger.LogInfo(message);
                        _statusNotifier.NotifyStatus(message);
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
                    _statusNotifier.NotifyStatus("All downloads and installations complete!");
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
