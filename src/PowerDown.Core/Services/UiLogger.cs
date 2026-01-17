using PowerDown.Abstractions.Interfaces;

namespace PowerDown.Core;

public class UiLogger : ILogger
{
    private readonly bool _verbose;
    public event Action<string>? OnMessageLogged;
    public event Action<string>? OnErrorLogged;

    public UiLogger(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void LogInfo(string message)
    {
        OnMessageLogged?.Invoke(message);
    }

    public void LogWarning(string message)
    {
        OnMessageLogged?.Invoke(message);
    }

    public void LogError(string message)
    {
        OnErrorLogged?.Invoke(message);
        OnMessageLogged?.Invoke(message);
    }

    public void LogVerbose(string message)
    {
        if (_verbose)
        {
            OnMessageLogged?.Invoke(message);
        }
    }

    public void LogSuccess(string message)
    {
        OnMessageLogged?.Invoke(message);
    }
}
