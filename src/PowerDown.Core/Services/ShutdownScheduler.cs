using System;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;

namespace PowerDown.Core.Services;

/// <summary>
/// Handles shutdown scheduling and cancellation
/// </summary>
public class ShutdownScheduler
{
    private readonly IShutdownService _shutdownService;
    private readonly ConsoleLogger _logger;
    private readonly Configuration _config;
    private readonly CancellationToken _cancellationToken;
    private bool _isVerificationPeriod = false;

    public bool IsVerificationPeriod => _isVerificationPeriod;

    public ShutdownScheduler(
        IShutdownService shutdownService,
        ConsoleLogger logger,
        Configuration config,
        CancellationToken cancellationToken)
    {
        _shutdownService = shutdownService ?? throw new ArgumentNullException(nameof(shutdownService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationToken = cancellationToken;
    }

    public async Task ScheduleShutdownAsync()
    {
        if (_config.DryRun)
        {
            _logger.LogInfo("Dry run mode: Would shutdown now");
            return;
        }

        var delaySeconds = _config.ShutdownDelaySeconds;
        _logger.LogInfo($"Initiating shutdown in {delaySeconds} seconds...");
        _logger.LogInfo("Press CTRL+C to cancel");
        await Task.Delay(delaySeconds * 1000, _cancellationToken);

        if (!_cancellationToken.IsCancellationRequested)
        {
            await _shutdownService.ScheduleShutdownAsync(delaySeconds, "PowerDown: All downloads complete");
            _logger.LogSuccess("Shutdown scheduled");
        }
    }

    public void SetVerificationPeriod(bool isActive)
    {
        _isVerificationPeriod = isActive;
    }

    public async Task CancelShutdownIfNeededAsync()
    {
        if (_isVerificationPeriod && !_config.DryRun)
        {
            _logger.LogInfo("Cancelling scheduled shutdown...");
            await _shutdownService.CancelShutdownAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger.LogWarning($"Failed to cancel shutdown: {t.Exception?.Message}");
                }
            }, TaskScheduler.Default);
        }
    }
}
