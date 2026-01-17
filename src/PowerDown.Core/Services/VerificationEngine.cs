using System;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Core.Services;

public class VerificationEngine
{
    private readonly DownloadMonitor _downloadMonitor;
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly CancellationToken _cancellationToken;
    private readonly IStatusNotifier _statusNotifier;

    public VerificationEngine(
        DownloadMonitor downloadMonitor,
        ILogger logger,
        Configuration config,
        CancellationToken cancellationToken,
        IStatusNotifier? statusNotifier = null)
    {
        _downloadMonitor = downloadMonitor ?? throw new ArgumentNullException(nameof(downloadMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationToken = cancellationToken;
        _statusNotifier = statusNotifier ?? new StatusNotifier();
    }

    public async Task RunVerificationPollingAsync()
    {
        var noActivityCount = 0;
        var elapsedTime = TimeSpan.Zero;
        var totalDelay = TimeSpan.FromSeconds(_config.VerificationDelaySeconds);
        var pollingInterval = TimeSpan.FromSeconds(_config.PollingIntervalSeconds);

        _logger.LogVerbose("Starting verification polling...");

        while (elapsedTime < totalDelay && !_cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollingInterval, _cancellationToken);
            elapsedTime += pollingInterval;

            try
            {
                var hasAnyActive = await _downloadMonitor.IsAnyDownloadActiveAsync();

                if (hasAnyActive)
                {
                    _logger.LogWarning($"Download resumed during verification - Resetting counter...");
                    _statusNotifier.NotifyStatus("Download resumed - Resetting verification counter...");
                    noActivityCount = 0;
                    await _downloadMonitor.WaitForAllDownloadsToCompleteAsync();
                }
                else
                {
                    noActivityCount++;
                    var message = $"Polling check {noActivityCount}/{_config.RequiredNoActivityChecks}: No activity detected";
                    _logger.LogInfo(message);
                    _statusNotifier.NotifyStatus(message);

                    if (noActivityCount >= _config.RequiredNoActivityChecks)
                    {
                        var finalProgress = new VerificationProgress
                        {
                            ChecksCompleted = noActivityCount,
                            TotalChecksRequired = _config.RequiredNoActivityChecks,
                            ElapsedTime = elapsedTime,
                            RemainingTime = totalDelay - elapsedTime,
                            NoActivityDetected = true
                        };
                        _statusNotifier.NotifyVerificationProgress(finalProgress);
                        _logger.LogSuccess("Verification complete - No downloads resumed!");
                        _statusNotifier.NotifyStatus("Verification complete!");
                        return;
                    }
                }

                var progress = new VerificationProgress
                {
                    ChecksCompleted = noActivityCount,
                    TotalChecksRequired = _config.RequiredNoActivityChecks,
                    ElapsedTime = elapsedTime,
                    RemainingTime = totalDelay - elapsedTime,
                    NoActivityDetected = !hasAnyActive
                };
                _statusNotifier.NotifyVerificationProgress(progress);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
