using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Core.Services;

public class DownloadOrchestrator : IDisposable
{
    private readonly IEnumerable<IDownloadDetector> _detectors;
    private readonly IShutdownService _shutdownService;
    private readonly ILogger _logger;
    private readonly Configuration _config;
    private readonly CancellationTokenSource _cts;
    private readonly IStatusNotifier _statusNotifier;

    public event Action? OnStarted;
    public event Action? OnCompleted;
    public event Action? OnCancelled;

    public DownloadOrchestrator(
        IEnumerable<IDownloadDetector> detectors,
        IShutdownService shutdownService,
        ILogger logger,
        Configuration config,
        IStatusNotifier? statusNotifier = null)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _shutdownService = shutdownService ?? throw new ArgumentNullException(nameof(shutdownService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cts = new CancellationTokenSource();
        _statusNotifier = statusNotifier ?? new StatusNotifier();

        Console.CancelKeyPress += OnCancelKeyPress;
    }

    public IStatusNotifier StatusNotifier => _statusNotifier;

    public async Task MonitorAndShutdownAsync(CancellationToken cancellationToken = default)
    {
        OnStarted?.Invoke();
        _statusNotifier.NotifyPhaseChange(new ApplicationPhase
        {
            CurrentPhase = Phase.Initializing,
            Description = "PowerDown is starting...",
            StartedAt = DateTime.Now
        });

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

        try
        {
            _logger.LogInfo($"PowerDown v1.0.0 starting...");
            _logger.LogInfo($"Verification delay: {_config.VerificationDelaySeconds}s, Polling interval: {_config.PollingIntervalSeconds}s, Required checks: {_config.RequiredNoActivityChecks}");
            _logger.LogInfo($"Monitoring Steam: {_config.MonitorSteam}");
            _logger.LogInfo($"Dry run mode: {_config.DryRun}");

            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.DetectingLaunchers,
                Description = "Detecting game launchers...",
                StartedAt = DateTime.Now
            });

            var downloadMonitor = new DownloadMonitor(_detectors, _logger, _config, token, _statusNotifier);
            var verificationEngine = new VerificationEngine(downloadMonitor, _logger, _config, token, _statusNotifier);
            var shutdownScheduler = new ShutdownScheduler(_shutdownService, _logger, _config, token, _statusNotifier);

            await downloadMonitor.InitializeDetectorsAsync();
            
            var initialDownloads = await downloadMonitor.GetActiveDownloadsAsync();
            downloadMonitor.DisplayActiveDownloads(initialDownloads);

            if (!initialDownloads.Any())
            {
                _logger.LogInfo("No downloads in progress - Waiting for downloads to start...");
                _statusNotifier.NotifyPhaseChange(new ApplicationPhase
                {
                    CurrentPhase = Phase.WaitingForDownloads,
                    Description = "Waiting for downloads to start...",
                    StartedAt = DateTime.Now
                });
                await downloadMonitor.WaitForDownloadsToStartAsync();
            }

            _logger.LogInfo("Monitoring downloads...");
            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.Monitoring,
                Description = "Monitoring downloads...",
                StartedAt = DateTime.Now
            });
            await downloadMonitor.WaitForAllDownloadsToCompleteAsync();

            _logger.LogSuccess("All downloads complete - Starting verification period...");
            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.Verifying,
                Description = "Verifying no new downloads...",
                StartedAt = DateTime.Now
            });
            shutdownScheduler.SetVerificationPeriod(true);
            await verificationEngine.RunVerificationPollingAsync();

            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.ShutdownPending,
                Description = "Scheduling shutdown...",
                StartedAt = DateTime.Now
            });
            await shutdownScheduler.ScheduleShutdownAsync();

            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.Completed,
                Description = "All downloads complete and verified!",
                StartedAt = DateTime.Now
            });
            
            OnCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Operation cancelled");
            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.Cancelled,
                Description = "Operation was cancelled",
                StartedAt = DateTime.Now
            });
            OnCancelled?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            _statusNotifier.NotifyError(ex.Message);
            _statusNotifier.NotifyPhaseChange(new ApplicationPhase
            {
                CurrentPhase = Phase.Error,
                Description = $"Error: {ex.Message}",
                StartedAt = DateTime.Now
            });
            throw;
        }
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger.LogInfo("Cancellation requested...");
        
        var downloadMonitor = new DownloadMonitor(_detectors, _logger, _config, _cts.Token, _statusNotifier);
        var shutdownScheduler = new ShutdownScheduler(_shutdownService, _logger, _config, _cts.Token, _statusNotifier);
        
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
