using PowerDown.Abstractions;

namespace PowerDown.Abstractions.Interfaces;

public interface IStatusNotifier
{
    event Action<string>? OnStatusMessage;
    event Action<DownloadUpdate>? OnDownloadUpdate;
    event Action<ApplicationPhase>? OnPhaseChange;
    event Action<VerificationProgress>? OnVerificationProgress;
    event Action<ShutdownEventArgs>? OnShutdownScheduled;
    event Action<string>? OnError;

    void NotifyStatus(string message);
    void NotifyDownloadUpdate(DownloadUpdate update);
    void NotifyPhaseChange(ApplicationPhase phase);
    void NotifyVerificationProgress(VerificationProgress progress);
    void NotifyShutdownScheduled(ShutdownEventArgs args);
    void NotifyError(string error);
}

public class DownloadUpdate
{
    public string GameName { get; set; } = string.Empty;
    public string LauncherName { get; set; } = string.Empty;
    public DownloadStatus Status { get; set; }
    public double Progress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApplicationPhase
{
    public Phase CurrentPhase { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public enum Phase
{
    Initializing,
    DetectingLaunchers,
    WaitingForDownloads,
    Monitoring,
    Verifying,
    ShutdownPending,
    Completed,
    Cancelled,
    Error
}

public class VerificationProgress
{
    public int ChecksCompleted { get; set; }
    public int TotalChecksRequired { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public bool NoActivityDetected { get; set; }
}

public class ShutdownEventArgs : EventArgs
{
    public int DelaySeconds { get; set; }
    public bool IsDryRun { get; set; }
    public string Reason { get; set; } = string.Empty;
}
