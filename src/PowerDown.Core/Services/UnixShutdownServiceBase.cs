using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PowerDown.Abstractions;

namespace PowerDown.Core.Services;

/// <summary>
/// Base class for Unix-based shutdown services (Linux and macOS).
/// Uses the 'shutdown' command for system shutdown operations.
/// </summary>
public abstract class UnixShutdownServiceBase : IShutdownService
{
    /// <summary>
    /// Gets the shutdown command executable name.
    /// </summary>
    protected virtual string ShutdownCommand => "shutdown";

    /// <inheritdoc />
    public bool IsShutdownScheduled { get; private set; }

    /// <inheritdoc />
    public virtual async Task ScheduleShutdownAsync(int delaySeconds, string message)
    {
        if (delaySeconds <= 0)
        {
            throw new ArgumentException("Delay must be greater than 0", nameof(delaySeconds));
        }

        var args = $"now +{delaySeconds} \"{message}\"";
        
        await ExecuteCommandAsync(args, exitCode => IsShutdownScheduled = exitCode == 0);
    }

    /// <inheritdoc />
    public virtual async Task CancelShutdownAsync()
    {
        await ExecuteCommandAsync("-c", exitCode => IsShutdownScheduled = exitCode != 0);
    }

    /// <summary>
    /// Executes a shutdown command.
    /// </summary>
    /// <param name="arguments">The command arguments.</param>
    /// <param name="onSuccess">Callback when command succeeds (receives exit code).</param>
    protected async Task ExecuteCommandAsync(string arguments, Action<int> onSuccess)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ShutdownCommand,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                onSuccess(process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to execute shutdown command: {ex.Message}");
        }
    }
}
