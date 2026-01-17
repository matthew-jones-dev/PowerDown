using System;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Core.Services;

public class ShutdownScheduler
{
    private readonly IShutdownService _shutdownService;
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly CancellationToken _cancellationToken;
    private readonly IStatusNotifier _statusNotifier;
    private bool _isVerificationPeriod = false;

    public bool IsVerificationPeriod => _isVerificationPeriod;

    public ShutdownScheduler(
        IShutdownService shutdownService,
        ILogger logger,
        Configuration config,
        CancellationToken cancellationToken,
        IStatusNotifier? statusNotifier = null)
    {
        _shutdownService = shutdownService ?? throw new ArgumentNullException(nameof(shutdownService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cancellationToken = cancellationToken;
        _statusNotifier = statusNotifier ?? new StatusNotifier();
    }

    public async Task ScheduleShutdownAsync()
    {
        if (_config.DryRun)
        {
            var message = "Dry run mode: Would shutdown now";
            _logger.LogInfo(message);
            _statusNotifier.NotifyStatus(message);
            _statusNotifier.NotifyShutdownScheduled(new ShutdownEventArgs
            {
                DelaySeconds = _config.ShutdownDelaySeconds,
                IsDryRun = true,
                Reason = "Dry run mode"
            });
            return;
        }

        var delaySeconds = _config.ShutdownDelaySeconds;
        var message2 = $"Initiating shutdown in {delaySeconds} seconds...";
        _logger.LogInfo(message2);
        _statusNotifier.NotifyStatus(message2);
        _statusNotifier.NotifyShutdownScheduled(new ShutdownEventArgs
        {
            DelaySeconds = delaySeconds,
            IsDryRun = false,
            Reason = "All downloads complete"
        });
        
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
                    var error = $"Failed to cancel shutdown: {t.Exception?.Message}";
                    _logger.LogWarning(error);
                    _statusNotifier.NotifyStatus(error);
                }
                else
                {
                    _statusNotifier.NotifyStatus("Shutdown cancelled");
                }
            }, TaskScheduler.Default);
        }
    }
}
