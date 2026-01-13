namespace PowerDown.Abstractions;

public interface IShutdownService
{
    Task ScheduleShutdownAsync(int delaySeconds, string message);
    Task CancelShutdownAsync();
    bool IsShutdownScheduled { get; }
}
