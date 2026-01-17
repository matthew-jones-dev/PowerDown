using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json;
using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;
using PowerDown.Core;
using PowerDown.Core.Services;
using PowerDown.Platform.Linux;
using PowerDown.Platform.Linux.Services;
using PowerDown.Platform.macOS;
using PowerDown.Platform.macOS.Services;
using PowerDown.Platform.Windows;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.UI.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action? _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<object?>? _executeWithParam;
    private readonly Func<object?, bool>? _canExecuteWithParam;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteWithParam = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_canExecuteWithParam != null)
        {
            return _canExecuteWithParam(parameter);
        }
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        if (_executeWithParam != null)
        {
            _executeWithParam(parameter);
            return;
        }
        _execute?.Invoke();
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Configuration _config;
    private readonly StatusNotifier _statusNotifier;
    private DownloadOrchestrator? _orchestrator;
    private CancellationTokenSource? _cts;
    private IShutdownService? _shutdownService;
    
    private bool _isMonitoring;
    private string _currentPhaseText = "Ready";
    private string _phaseDescription = "Click 'Start Monitoring' to begin";
    private string _statusText = "Waiting to start";
    private string _statusSubtext = "";
    private string _statusIndicatorColor = "#4CAF50";
    private bool _showVerificationProgress;
    private int _verificationChecksCompleted;
    private int _verificationTotalChecks = 3;
    private string _verificationStatusText = "";
    private bool _showShutdownWarning;
    private string _shutdownCountdownText = "";
    private DispatcherTimer? _shutdownCountdownTimer;
    private DateTimeOffset? _shutdownCountdownDeadline;
    private bool _shutdownCountdownIsDryRun;
    private DateTime _lastUpdateTime = DateTime.Now;
    
    public ObservableCollection<DownloadItemViewModel> ActiveDownloads { get; } = new();
    
    public bool MonitorSteam
    {
        get => _config.MonitorSteam;
        set { _config.MonitorSteam = value; OnPropertyChanged(); }
    }
    
    
    public int VerificationDelaySeconds
    {
        get => _config.VerificationDelaySeconds;
        set { _config.VerificationDelaySeconds = value; OnPropertyChanged(); }
    }
    
    public int PollingIntervalSeconds
    {
        get => _config.PollingIntervalSeconds;
        set { _config.PollingIntervalSeconds = value; OnPropertyChanged(); }
    }
    
    public int RequiredNoActivityChecks
    {
        get => _config.RequiredNoActivityChecks;
        set
        {
            _config.RequiredNoActivityChecks = value;
            VerificationTotalChecks = value;
            OnPropertyChanged();
        }
    }
    
    public int ShutdownDelaySeconds
    {
        get => _config.ShutdownDelaySeconds;
        set { _config.ShutdownDelaySeconds = value; OnPropertyChanged(); }
    }
    
    public bool DryRun
    {
        get => _config.DryRun;
        set { _config.DryRun = value; OnPropertyChanged(); }
    }
    
    public string CustomSteamPath
    {
        get => _config.CustomSteamPath ?? "";
        set { _config.CustomSteamPath = value; OnPropertyChanged(); }
    }
    
    
    public bool HasActiveDownloads => ActiveDownloads.Any(d => d.IsActive);

    public bool HasAnyDownloads => ActiveDownloads.Count > 0;
    
    public bool CanStart => !_isMonitoring;
    
    public bool CanCancel => _isMonitoring;
    
    public ICommand StartCommand { get; }
    
    public ICommand CancelCommand { get; }

    public ICommand SaveSettingsCommand { get; }

    public ICommand CancelShutdownCommand { get; }

    public ICommand RemoveDownloadCommand { get; }
    
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _saveSettingsCommand;
    private readonly RelayCommand _cancelShutdownCommand;
    private readonly RelayCommand _removeDownloadCommand;
    
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set
        {
            _isMonitoring = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanCancel));
            _startCommand.RaiseCanExecuteChanged();
            _cancelCommand.RaiseCanExecuteChanged();
        }
    }
    
    public string CurrentPhaseText
    {
        get => _currentPhaseText;
        set { _currentPhaseText = value; OnPropertyChanged(); }
    }
    
    public string PhaseDescription
    {
        get => _phaseDescription;
        set { _phaseDescription = value; OnPropertyChanged(); }
    }
    
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }
    
    public string StatusSubtext
    {
        get => _statusSubtext;
        set { _statusSubtext = value; OnPropertyChanged(); }
    }
    
    public string StatusIndicatorColor
    {
        get => _statusIndicatorColor;
        set { _statusIndicatorColor = value; OnPropertyChanged(); }
    }
    
    public bool ShowVerificationProgress
    {
        get => _showVerificationProgress;
        set { _showVerificationProgress = value; OnPropertyChanged(); }
    }
    
    public int VerificationChecksCompleted
    {
        get => _verificationChecksCompleted;
        set { _verificationChecksCompleted = value; OnPropertyChanged(); }
    }
    
    public int VerificationTotalChecks
    {
        get => _verificationTotalChecks;
        set { _verificationTotalChecks = value; OnPropertyChanged(); }
    }
    
    public string VerificationStatusText
    {
        get => _verificationStatusText;
        set { _verificationStatusText = value; OnPropertyChanged(); }
    }
    
    public bool ShowShutdownWarning
    {
        get => _showShutdownWarning;
        set
        {
            _showShutdownWarning = value;
            OnPropertyChanged();
            _cancelShutdownCommand?.RaiseCanExecuteChanged();
        }
    }
    
    public string ShutdownCountdownText
    {
        get => _shutdownCountdownText;
        set { _shutdownCountdownText = value; OnPropertyChanged(); }
    }
    
    public string LastUpdateTime
    {
        get => "Last update: " + _lastUpdateTime.ToString("HH:mm:ss");
        set { }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public MainViewModel()
    {
        _config = LoadConfiguration();
        _statusNotifier = new StatusNotifier();
        VerificationTotalChecks = _config.RequiredNoActivityChecks;
        
        _startCommand = new RelayCommand(async () => await StartMonitoringAsync(), () => !_isMonitoring);
        _cancelCommand = new RelayCommand(CancelMonitoring, () => _isMonitoring);
        _saveSettingsCommand = new RelayCommand(SaveSettings);
        _cancelShutdownCommand = new RelayCommand(async () => await CancelShutdownAsync(), () => _showShutdownWarning);
        _removeDownloadCommand = new RelayCommand(RemoveDownload);
        StartCommand = _startCommand;
        CancelCommand = _cancelCommand;
        SaveSettingsCommand = _saveSettingsCommand;
        CancelShutdownCommand = _cancelShutdownCommand;
        RemoveDownloadCommand = _removeDownloadCommand;
    }
    
    private static Configuration LoadConfiguration()
    {
        var config = new Configuration();
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "POWERDOWN_");
        
        var configuration = builder.Build();
        var section = configuration.GetSection("PowerDown");
        if (section.Exists())
        {
            section.Bind(config);
        }
        
        return config;
    }
    
    private void SubscribeToStatusNotifications()
    {
        _statusNotifier.OnStatusMessage += OnStatusMessage;
        _statusNotifier.OnDownloadUpdate += OnDownloadUpdate;
        _statusNotifier.OnPhaseChange += OnPhaseChange;
        _statusNotifier.OnVerificationProgress += OnVerificationProgress;
        _statusNotifier.OnShutdownScheduled += OnShutdownScheduled;
        _statusNotifier.OnError += OnError;
    }
    
    private async Task StartMonitoringAsync()
    {
        if (_isMonitoring) return;
        
        SubscribeToStatusNotifications();
        
        IsMonitoring = true;
        ActiveDownloads.Clear();
        _cts = new CancellationTokenSource();
        
        try
        {
            var serviceProvider = BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            
            var detectors = serviceProvider.GetRequiredService<IDetectorFactory>().CreateDetectors(_config, logger);
            var shutdownService = serviceProvider.GetRequiredService<IShutdownService>();
            _shutdownService = shutdownService;
            
            var orchestrator = new DownloadOrchestrator(
                detectors,
                shutdownService,
                logger,
                _config,
                _statusNotifier);
            
            _orchestrator = orchestrator;
            
            orchestrator.OnCompleted += OnMonitoringCompleted;
            orchestrator.OnCancelled += OnMonitoringCancelled;
            
            await orchestrator.MonitorAndShutdownAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusText = "Error occurred";
                StatusSubtext = ex.Message;
                StatusIndicatorColor = "#F44336";
                IsMonitoring = false;
            });
        }
    }
    
    private void CancelMonitoring()
    {
        _cts?.Cancel();
        IsMonitoring = false;
        _shutdownService = null;
        ShowShutdownWarning = false;
        StopShutdownCountdown();
        CurrentPhaseText = "Cancelled";
        PhaseDescription = "Monitoring has been cancelled";
        StatusText = "Cancelled";
        StatusSubtext = "Click 'Start Monitoring' to try again";
        StatusIndicatorColor = "#FFC107";
    }
    
    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        
        var uiLogger = new UiLogger();
        uiLogger.OnMessageLogged += m => Dispatcher.UIThread.Post(() => StatusSubtext = m);
        services.AddSingleton<ILogger>(uiLogger);
        services.AddSingleton(_config);
        
        if (OperatingSystem.IsWindows())
        {
            ConfigureWindowsServices(services);
        }
        else if (OperatingSystem.IsLinux())
        {
            services.AddSingleton<LinuxPlatformDetector>();
            services.AddSingleton<ISteamPathDetector, LinuxSteamPathDetector>();
            services.AddSingleton<LinuxShutdownService>();
            services.AddSingleton<IShutdownService>(sp => sp.GetRequiredService<LinuxShutdownService>());
            services.AddSingleton<IDetectorFactory, LinuxDetectorFactory>();
        }
        else if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<MacPlatformDetector>();
            services.AddSingleton<ISteamPathDetector, MacSteamPathDetector>();
            services.AddSingleton<MacShutdownService>();
            services.AddSingleton<IShutdownService>(sp => sp.GetRequiredService<MacShutdownService>());
            services.AddSingleton<IDetectorFactory, MacDetectorFactory>();
        }
        else
        {
            throw new PlatformNotSupportedException("This application is only supported on Windows, Linux, and macOS.");
        }
        
        return services.BuildServiceProvider();
    }

    [SupportedOSPlatform("windows")]
    private static void ConfigureWindowsServices(IServiceCollection services)
    {
        services.AddSingleton<WindowsPlatformDetector>();
        services.AddSingleton<ISteamPathDetector, SteamPathDetector>();
        services.AddSingleton<WindowsShutdownService>();
        services.AddSingleton<IShutdownService>(sp => sp.GetRequiredService<WindowsShutdownService>());
        services.AddSingleton<IDetectorFactory, DetectorFactory>();
    }
    
    private void OnStatusMessage(string message)
    {
        var trimmed = message.TrimStart();
        if (trimmed.StartsWith("- ") || message.StartsWith("Polling check") || message.StartsWith("Active Downloads"))
        {
            UpdateTimestamp();
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            StatusSubtext = message;
            UpdateTimestamp();
        });
    }
    
    private void OnDownloadUpdate(DownloadUpdate update)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsDisplayableDownload(update.GameName))
            {
                UpdateTimestamp();
                return;
            }

            var existing = ActiveDownloads.FirstOrDefault(d => d.GameName == update.GameName && d.LauncherName == update.LauncherName);
            
            if (existing != null)
            {
                existing.UpdateFrom(update);
            }
            else
            {
                var item = new DownloadItemViewModel(update);
                ActiveDownloads.Add(item);
                OnPropertyChanged(nameof(HasActiveDownloads));
                OnPropertyChanged(nameof(HasAnyDownloads));
            }
            
            OnPropertyChanged(nameof(HasActiveDownloads));
            UpdateTimestamp();
        });
    }

    private static bool IsDisplayableDownload(string gameName)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            return false;
        }

        if (gameName.StartsWith("AppID ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (gameName.Equals("Unknown Game", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (gameName.Contains("secs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
    
    private void OnPhaseChange(ApplicationPhase phase)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentPhaseText = phase.CurrentPhase.ToString();
            PhaseDescription = phase.Description;
            ShowVerificationProgress = phase.CurrentPhase == Phase.Verifying;
            
            StatusIndicatorColor = phase.CurrentPhase switch
            {
                Phase.Initializing or Phase.DetectingLaunchers or Phase.WaitingForDownloads => "#2196F3",
                Phase.Monitoring => "#4CAF50",
                Phase.Verifying => "#FF9800",
                Phase.ShutdownPending => "#F44336",
                Phase.Completed => "#4CAF50",
                Phase.Cancelled => "#FFC107",
                Phase.Error => "#F44336",
                _ => "#4CAF50"
            };
            
            UpdateTimestamp();
        });
    }
    
    private void OnVerificationProgress(VerificationProgress progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ShowVerificationProgress = true;
            VerificationChecksCompleted = progress.ChecksCompleted;
            VerificationTotalChecks = progress.TotalChecksRequired;
            VerificationStatusText = $"Check {progress.ChecksCompleted}/{progress.TotalChecksRequired} - {progress.ElapsedTime.TotalSeconds:F0}s elapsed";
            
            if (progress.NoActivityDetected)
            {
                StatusText = "Verifying downloads are complete...";
                StatusSubtext = $"{progress.TotalChecksRequired - progress.ChecksCompleted} checks remaining";
            }
            
            UpdateTimestamp();
        });
    }
    
    private void OnShutdownScheduled(ShutdownEventArgs args)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ShowShutdownWarning = true;
            StartShutdownCountdown(args.DelaySeconds, args.IsDryRun);
            
            if (args.IsDryRun)
            {
                StatusText = "Dry run mode - would shutdown now";
                StatusSubtext = "No actual shutdown will occur";
                StatusIndicatorColor = "#2196F3";
            }
            else
            {
                StatusText = "Shutdown imminent!";
                StatusSubtext = $"{args.DelaySeconds} seconds until shutdown";
                StatusIndicatorColor = "#F44336";
            }
            
            UpdateTimestamp();
        });
    }
    
    private void OnError(string error)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText = "Error";
            StatusSubtext = error;
            StatusIndicatorColor = "#F44336";
            UpdateTimestamp();
        });
    }
    
    private void OnMonitoringCompleted()
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsMonitoring = false;
            ShowVerificationProgress = false;
            CurrentPhaseText = "Complete";
            PhaseDescription = "All downloads verified - shutdown scheduled";
            StatusText = "Done!";
            StatusSubtext = "Your PC will shut down shortly";
        });
    }
    
    private void OnMonitoringCancelled()
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsMonitoring = false;
            ShowShutdownWarning = false;
            StopShutdownCountdown();
            ShowVerificationProgress = false;
            _shutdownService = null;
            CurrentPhaseText = "Cancelled";
            PhaseDescription = "Monitoring was cancelled";
            StatusText = "Cancelled";
            StatusSubtext = "Click 'Start Monitoring' to try again";
            StatusIndicatorColor = "#FFC107";
        });
    }

    private void SaveSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var settings = new
        {
            PowerDown = new
            {
                VerificationDelaySeconds = _config.VerificationDelaySeconds,
                PollingIntervalSeconds = _config.PollingIntervalSeconds,
                RequiredNoActivityChecks = _config.RequiredNoActivityChecks,
                ShutdownDelaySeconds = _config.ShutdownDelaySeconds,
                MonitorSteam = _config.MonitorSteam,
                DryRun = _config.DryRun,
                Verbose = _config.Verbose,
                CustomSteamPath = string.IsNullOrWhiteSpace(_config.CustomSteamPath) ? "" : _config.CustomSteamPath
            }
        };

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        StatusSubtext = "Settings saved";
        UpdateTimestamp();
    }

    private async Task CancelShutdownAsync()
    {
        if (_shutdownService == null)
        {
            return;
        }

        try
        {
            await _shutdownService.CancelShutdownAsync();
            ShowShutdownWarning = false;
            StopShutdownCountdown();
            StatusText = "Shutdown cancelled";
            StatusSubtext = "Monitoring will continue";
            StatusIndicatorColor = "#2196F3";
        }
        catch (Exception ex)
        {
            StatusText = "Failed to cancel shutdown";
            StatusSubtext = ex.Message;
            StatusIndicatorColor = "#F44336";
        }
        finally
        {
            UpdateTimestamp();
        }
    }

    private void RemoveDownload(object? parameter)
    {
        if (parameter is not DownloadItemViewModel item)
        {
            return;
        }

        ActiveDownloads.Remove(item);
        OnPropertyChanged(nameof(HasActiveDownloads));
        OnPropertyChanged(nameof(HasAnyDownloads));
    }
    
    private void UpdateTimestamp()
    {
        _lastUpdateTime = DateTime.Now;
        OnPropertyChanged(nameof(LastUpdateTime));
    }

    private void StartShutdownCountdown(int delaySeconds, bool isDryRun)
    {
        _shutdownCountdownDeadline = DateTimeOffset.Now.AddSeconds(delaySeconds);
        _shutdownCountdownIsDryRun = isDryRun;

        if (_shutdownCountdownTimer == null)
        {
            _shutdownCountdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _shutdownCountdownTimer.Tick += (_, _) => UpdateShutdownCountdown();
        }

        UpdateShutdownCountdown();
        if (!_shutdownCountdownTimer.IsEnabled)
        {
            _shutdownCountdownTimer.Start();
        }
    }

    private void StopShutdownCountdown()
    {
        _shutdownCountdownTimer?.Stop();
        _shutdownCountdownDeadline = null;
    }

    private void UpdateShutdownCountdown()
    {
        if (_shutdownCountdownDeadline == null)
        {
            return;
        }

        var remaining = _shutdownCountdownDeadline.Value - DateTimeOffset.Now;
        var secondsLeft = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));

        if (_shutdownCountdownIsDryRun)
        {
            ShutdownCountdownText = $"Dry run: would shut down in {secondsLeft} seconds";
        }
        else
        {
            ShutdownCountdownText = $"Shutting down in {secondsLeft} seconds";
            StatusSubtext = $"{secondsLeft} seconds until shutdown";
        }

        if (secondsLeft == 0)
        {
            StopShutdownCountdown();
        }

        UpdateTimestamp();
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
