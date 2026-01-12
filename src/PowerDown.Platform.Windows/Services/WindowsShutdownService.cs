using System;
using System.Diagnostics;
using PowerDown.Abstractions;

namespace PowerDown.Platform.Windows.Services;

public class WindowsShutdownService : IShutdownService
{
    public bool IsShutdownScheduled { get; private set; }

    public async Task ScheduleShutdownAsync(int delaySeconds, string message)
    {
        if (delaySeconds <= 0)
        {
            throw new ArgumentException("Delay must be greater than 0", nameof(delaySeconds));
        }

        var args = $"/s /t {delaySeconds}";
        if (!string.IsNullOrWhiteSpace(message))
        {
            args += $" /c \"{message}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(startInfo);
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            IsShutdownScheduled = true;
        }
    }

    public async Task CancelShutdownAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = "/a",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(startInfo);
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            IsShutdownScheduled = false;
        }
    }
}
