using PowerDown.Abstractions;
using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Core;

public class StatusNotifier : IStatusNotifier
{
    public event Action<string>? OnStatusMessage;
    public event Action<DownloadUpdate>? OnDownloadUpdate;
    public event Action<ApplicationPhase>? OnPhaseChange;
    public event Action<VerificationProgress>? OnVerificationProgress;
    public event Action<ShutdownEventArgs>? OnShutdownScheduled;
    public event Action<string>? OnError;

    public void NotifyStatus(string message)
    {
        OnStatusMessage?.Invoke(message);
    }

    public void NotifyDownloadUpdate(DownloadUpdate update)
    {
        OnDownloadUpdate?.Invoke(update);
    }

    public void NotifyPhaseChange(ApplicationPhase phase)
    {
        OnPhaseChange?.Invoke(phase);
    }

    public void NotifyVerificationProgress(VerificationProgress progress)
    {
        OnVerificationProgress?.Invoke(progress);
    }

    public void NotifyShutdownScheduled(ShutdownEventArgs args)
    {
        OnShutdownScheduled?.Invoke(args);
    }

    public void NotifyError(string error)
    {
        OnError?.Invoke(error);
    }
}
