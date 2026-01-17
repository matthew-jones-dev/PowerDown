namespace PowerDown.Abstractions.Interfaces;

public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogVerbose(string message);
    void LogSuccess(string message);
}
