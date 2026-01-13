# PowerDown API Documentation

This document describes the public API of PowerDown, including interfaces, models, and extension points for developers who want to extend the system.

## Core Interfaces

### IDownloadDetector

Interface for detecting game downloads from a launcher.

```csharp
public interface IDownloadDetector
{
    string LauncherName { get; }
    Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync();
    Task<bool> IsAnyDownloadOrInstallActiveAsync();
    Task<bool> InitializeAsync();
}
```

**Properties:**

- `LauncherName` - Name of the launcher (e.g., "Steam", "Epic Games")

**Methods:**

- `InitializeAsync()` - One-time initialization for the detector
  - Returns: `Task<bool>` - True if initialization succeeded
  - Throws: May throw if required paths don't exist

- `GetActiveDownloadsAsync()` - Get list of currently active downloads
  - Returns: `Task<IEnumerable<GameDownloadInfo>>` - List of active downloads
  - Throws: May throw if file access fails

- `IsAnyDownloadOrInstallActiveAsync()` - Quick check if any download/install is active
  - Returns: `Task<bool>` - True if any activity detected
  - Throws: May throw if file access fails

**Usage Example:**

```csharp
var detector = new SteamDownloadDetector(steamPath);
await detector.InitializeAsync();

var downloads = await detector.GetActiveDownloadsAsync();
foreach (var download in downloads)
{
    Console.WriteLine($"{download.GameName}: {download.Progress}%");
}

var hasActivity = await detector.IsAnyDownloadOrInstallActiveAsync();
if (hasActivity)
{
    Console.WriteLine("Downloads in progress");
}
```

---

### IShutdownService

Interface for scheduling system shutdown.

```csharp
public interface IShutdownService
{
    Task ScheduleShutdownAsync(int delaySeconds, string message);
    Task CancelShutdownAsync();
    bool IsShutdownScheduled { get; }
}
```

**Properties:**

- `IsShutdownScheduled` - Indicates if a shutdown has been scheduled

**Methods:**

- `ScheduleShutdownAsync(delaySeconds, message)` - Schedule system shutdown
  - `delaySeconds` - Delay before shutdown (must be > 0)
  - `message` - Message to display before shutdown
  - Returns: `Task`
  - Throws: `ArgumentException` if delay is <= 0

- `CancelShutdownAsync()` - Cancel any scheduled shutdown
  - Returns: `Task`

**Usage Example:**

```csharp
var shutdownService = new WindowsShutdownService();

await shutdownService.ScheduleShutdownAsync(60, "Updates complete");
Console.WriteLine("Shutdown scheduled in 60 seconds");

await shutdownService.CancelShutdownAsync();
Console.WriteLine("Shutdown cancelled");
```

---

### IPlatformDetector

Interface for detecting the current platform.

```csharp
public interface IPlatformDetector
{
    bool IsSupported();
    string GetPlatformName();
}
```

**Methods:**

- `IsSupported()` - Check if this platform is supported
  - Returns: `bool` - True if platform matches current OS

- `GetPlatformName()` - Get the platform name
  - Returns: `string` - Platform name (e.g., "Windows", "Linux")

**Usage Example:**

```csharp
var detector = new WindowsPlatformDetector();

if (detector.IsSupported())
{
    Console.WriteLine($"Running on {detector.GetPlatformName()}");
}
```

---

## Models

### GameDownloadInfo

Represents a game's download and installation state.

```csharp
public class GameDownloadInfo
{
    public string GameName { get; set; }
    public DownloadStatus DownloadStatus { get; set; }
    public DownloadStatus InstallStatus { get; set; }
    public double Progress { get; set; }
    public string LauncherName { get; set; }
}
```

**Properties:**

- `GameName` - Name of the game
- `DownloadStatus` - Current download status
- `InstallStatus` - Current installation status
- `Progress` - Progress percentage (0-100)
- `LauncherName` - Name of the launcher

**Usage Example:**

```csharp
var info = new GameDownloadInfo
{
    GameName = "Counter-Strike: Global Offensive",
    DownloadStatus = DownloadStatus.Downloading,
    InstallStatus = DownloadStatus.Unknown,
    Progress = 78.5,
    LauncherName = "Steam"
};

Console.WriteLine($"{info.GameName}: {info.DownloadStatus} ({info.Progress}%)");
```

---

### DownloadStatus

Enum for download and installation status states.

```csharp
public enum DownloadStatus
{
    Downloading,
    Installing,
    Idle,
    Unknown,
    Error
}
```

**Values:**

- `Downloading` - Game is currently being downloaded
- `Installing` - Game is being installed after download
- `Idle` - No download or installation activity
- `Unknown` - Status cannot be determined
- `Error` - An error occurred

**Usage Example:**

```csharp
if (downloadInfo.DownloadStatus == DownloadStatus.Downloading)
{
    Console.WriteLine("Download in progress");
}
else if (downloadInfo.DownloadStatus == DownloadStatus.Idle)
{
    Console.WriteLine("No activity");
}
```

---

### Configuration

Application configuration model.

```csharp
public class Configuration
{
    public int VerificationDelaySeconds { get; set; }
    public int PollingIntervalSeconds { get; set; }
    public int RequiredNoActivityChecks { get; set; }
    public bool MonitorSteam { get; set; }
    public bool MonitorEpic { get; set; }
    public bool DryRun { get; set; }
    public bool Verbose { get; set; }
    public string? CustomSteamPath { get; set; }
    public string? CustomEpicPath { get; set; }
}
```

**Properties:**

- `VerificationDelaySeconds` - Total verification delay in seconds (default: 60)
- `PollingIntervalSeconds` - Polling interval in seconds (default: 10)
- `RequiredNoActivityChecks` - Required consecutive idle checks (default: 3)
- `MonitorSteam` - Enable Steam monitoring (default: true)
- `MonitorEpic` - Enable Epic Games monitoring (default: true)
- `DryRun` - Test mode without actual shutdown (default: false)
- `Verbose` - Enable verbose logging (default: false)
- `CustomSteamPath` - Custom Steam install directory (default: null)
- `CustomEpicPath` - Custom Epic install directory (default: null)

**Usage Example:**

```csharp
var config = new Configuration
{
    VerificationDelaySeconds = 120,
    PollingIntervalSeconds = 15,
    RequiredNoActivityChecks = 5,
    MonitorSteam = true,
    MonitorEpic = false,
    DryRun = true,
    Verbose = true
};
```

---

## Extension Points

### Adding a New Platform

To add support for a new platform (e.g., Linux), follow these steps:

1. **Create Platform Project**
   - Create `PowerDown.Platform.Linux` project
   - Reference `PowerDown.Abstractions`

2. **Implement IPlatformDetector**

```csharp
public class LinuxPlatformDetector : IPlatformDetector
{
    public bool IsSupported() => OperatingSystem.IsLinux();
    public string GetPlatformName() => "Linux";
}
```

3. **Implement IShutdownService**

```csharp
public class LinuxShutdownService : IShutdownService
{
    public bool IsShutdownScheduled { get; private set; }

    public async Task ScheduleShutdownAsync(int delaySeconds, string message)
    {
        var args = $"-h +{delaySeconds}";
        if (!string.IsNullOrWhiteSpace(message))
        {
            args += $" \"{message}\"";
        }

        var process = Process.Start("shutdown", args);
        await process.WaitForExitAsync();

        IsShutdownScheduled = true;
    }

    public async Task CancelShutdownAsync()
    {
        var process = Process.Start("shutdown", "-c");
        await process.WaitForExitAsync();

        IsShutdownScheduled = false;
    }
}
```

4. **Implement IDownloadDetector for Each Launcher**

```csharp
public class SteamDownloadDetectorLinux : IDownloadDetector
{
    public string LauncherName => "Steam";
    
    public async Task<bool> InitializeAsync()
    {
        // Linux-specific initialization
    }

    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        // Linux-specific download detection
    }

    public async Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        // Linux-specific activity detection
    }
}
```

5. **Register in DI Container**

Update `PowerDown.Cli/Program.cs` to register Linux services:

```csharp
services.AddSingleton<LinuxPlatformDetector>();
services.AddSingleton<LinuxShutdownService>();
```

---

### Adding a New Launcher

To add support for a new game launcher (e.g., GOG Galaxy), follow these steps:

1. **Create Detector Class**

```csharp
public class GOGDownloadDetector : IDownloadDetector
{
    private readonly string? _gogPath;

    public string LauncherName => "GOG Galaxy";

    public GOGDownloadDetector(string? gogPath)
    {
        _gogPath = gogPath;
    }

    public async Task<bool> InitializeAsync()
    {
        // Detect GOG path or use provided path
        // Initialize file watchers
        return true;
    }

    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        // Parse GOG logs or manifests
        // Return list of active downloads
    }

    public async Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        // Quick check for activity
        var downloads = await GetActiveDownloadsAsync();
        return downloads.Any(d => d.DownloadStatus == DownloadStatus.Downloading ||
                               d.InstallStatus == DownloadStatus.Installing);
    }
}
```

2. **Create Path Detector (Optional)**

```csharp
public class GOGPathDetector
{
    public static string? DetectGOGPath(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return customPath;
        }

        // Auto-detect GOG installation path
        // Check registry, config files, or common locations
        
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "GOG Galaxy");

        return Directory.Exists(defaultPath) ? defaultPath : null;
    }
}
```

3. **Register in DI Container**

Update `PowerDown.Cli/Program.cs`:

```csharp
var detectors = new List<IDownloadDetector>();

// Add to configuration
public bool MonitorGOG { get; set; } = true;
public string? CustomGOGPath { get; set; }

// In DI setup
if (config.MonitorGOG)
{
    var gogPath = GOGPathDetector.DetectGOGPath(config.CustomGOGPath);
    if (gogPath != null)
    {
        detectors.Add(new GOGDownloadDetector(gogPath));
    }
}
```

---

## Thread Safety

### IDownloadDetector

Implementations should be thread-safe if they will be called concurrently. For current implementation, orchestrator calls detectors sequentially, so thread safety is not required.

### IShutdownService

`ScheduleShutdownAsync` and `CancelShutdownAsync` should be thread-safe. Current implementation synchronizes through process execution.

---

## Error Handling

### Expected Exceptions

**IDownloadDetector:**

- `DirectoryNotFoundException` - Required directory doesn't exist
- `FileNotFoundException` - Required file doesn't exist
- `IOException` - File access error
- `UnauthorizedAccessException` - Insufficient permissions

**IShutdownService:**

- `ArgumentException` - Invalid delay parameter
- `InvalidOperationException` - Shutdown already scheduled (may vary by platform)
- `Win32Exception` - Windows-specific error (Windows platform only)

### Best Practices

1. **Graceful Degradation** - If one detector fails, others should continue working
2. **User-Friendly Messages** - Provide clear error messages with suggestions
3. **Logging** - Log errors before throwing to help debugging
4. **Null Checks** - Validate all input parameters

---

## Versioning

### API Stability

PowerDown follows Semantic Versioning (SemVer):

- **Major (X.0.0)** - Breaking changes to API
- **Minor (0.X.0)** - New features, backward compatible
- **Patch (0.0.X)** - Bug fixes, backward compatible

### Deprecation Policy

Deprecated APIs will be marked with `[Obsolete]` attribute and supported for at least one major version before removal.

---

## Examples

### Complete Example: Custom Detector

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PowerDown.Abstractions;

public class CustomLauncherDetector : IDownloadDetector
{
    private readonly string _logPath;
    private readonly Dictionary<string, GameDownloadInfo> _downloads = new();

    public string LauncherName => "Custom Launcher";

    public CustomLauncherDetector(string installPath)
    {
        _logPath = Path.Combine(installPath, "downloads.log");
    }

    public async Task<bool> InitializeAsync()
    {
        if (!File.Exists(_logPath))
        {
            return false;
        }

        await ParseLogAsync();
        return true;
    }

    public async Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        await ParseLogAsync();
        return _downloads.Values.ToList();
    }

    public async Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        var downloads = await GetActiveDownloadsAsync();
        return downloads.Any(d => d.DownloadStatus == DownloadStatus.Downloading);
    }

    private async Task ParseLogAsync()
    {
        var content = await File.ReadAllTextAsync(_logPath);
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains("DOWNLOADING"))
            {
                var gameName = ExtractGameName(line);
                _downloads[gameName] = new GameDownloadInfo
                {
                    GameName = gameName,
                    LauncherName = LauncherName,
                    DownloadStatus = DownloadStatus.Downloading,
                    InstallStatus = DownloadStatus.Unknown,
                    Progress = 50.0
                };
            }
            else if (line.Contains("COMPLETE"))
            {
                var gameName = ExtractGameName(line);
                _downloads.Remove(gameName);
            }
        }
    }

    private string ExtractGameName(string line)
    {
        // Extract game name from log line
        // Implementation depends on log format
        return "Game Name";
    }
}
```

---

## Support

For questions about the API or extension development, please:

1. Read [ARCHITECTURE.md](ARCHITECTURE.md) for design details
2. Read [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) for architectural rationale
3. Read [DEVELOPMENT.md](DEVELOPMENT.md) for development guidelines
4. Open an issue on GitHub for specific questions
