using System;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;

namespace PowerDown.Core.Services;

/// <summary>
/// Handles verification polling after downloads complete
/// </summary>
public class VerificationEngine
{
    private readonly DownloadMonitor _downloadMonitor;
    private readonly ConsoleLogger _logger;
    private readonly Configuration _config;
    private readonly CancellationToken _cancellationToken;

    public VerificationEngine(
        DownloadMonitor downloadMonitor,
        ConsoleLogger logger,
        Configuration config,
        CancellationToken cancellationToken)
    {
        _downloadMonitor = downloadMonitor ?? throw new ArgumentNullException(nameof(downloadMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationToken = cancellationToken;
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
                    noActivityCount = 0;
                    await _downloadMonitor.WaitForAllDownloadsToCompleteAsync();
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
}
