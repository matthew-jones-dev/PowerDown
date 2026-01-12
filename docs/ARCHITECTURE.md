# PowerDown Architecture

This document describes the technical architecture of PowerDown, including component design, data flow, and key patterns.

## System Architecture

PowerDown follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      CLI Layer                            │
│                (PowerDown.Cli)                        │
│  - Command-line parsing                                 │
│  - Dependency injection setup                              │
│  - User interaction (Console output)                       │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                     Core Layer                           │
│               (PowerDown.Core)                            │
│  - Business logic orchestration                           │
│  - Configuration management                               │
│  - Logging                                             │
│  - Download coordination                                  │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                  Abstraction Layer                        │
│            (PowerDown.Abstractions)                        │
│  - Platform-agnostic interfaces                         │
│  - Shared models and enums                              │
│  - Extension points for new platforms/launchers          │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                  Platform Layer                           │
│          (PowerDown.Platform.Windows)                      │
│  - Platform-specific implementations                      │
│  - OS-specific path detection                            │
│  - Launcher-specific download detection                   │
│  - System shutdown (Windows-specific)                     │
└─────────────────────────────────────────────────────────────┘
```

## Layer Structure

### 1. Abstraction Layer (`PowerDown.Abstractions`)

**Purpose:** Defines platform-agnostic contracts and models

**Components:**
- `IDownloadDetector` - Interface for launcher download detection
- `IShutdownService` - Interface for system shutdown operations
- `IPlatformDetector` - Interface for platform detection
- `DownloadStatus` - Enum for download/installation states
- `GameDownloadInfo` - Model for game download information

**Key Design:** All platform-specific code depends on these abstractions, enabling easy addition of new platforms.

### 2. Core Layer (`PowerDown.Core`)

**Purpose:** Contains shared business logic and orchestration

**Components:**
- `Configuration` - Application settings model
- `ConsoleLogger` - Console output with colored logging
- `DownloadOrchestrator` - Coordinates multiple detectors and manages verification

**Key Design:** Platform-agnostic logic that works with any `IDownloadDetector` implementation.

### 3. Platform Layer (`PowerDown.Platform.Windows`)

**Purpose:** Windows-specific implementations

**Components:**
- `WindowsPlatformDetector` - Windows OS detection
- `WindowsShutdownService` - Windows shutdown via `shutdown.exe`
- `SteamPathDetector` - Steam path detection (registry + fallback)
- `EpicPathDetector` - Epic path detection (JSON + fallback)
- `SteamDownloadDetector` - Steam download detection
- `EpicDownloadDetector` - Epic download detection

**Key Design:** All Windows-specific code isolated here. Other platforms would have their own projects.

### 4. CLI Layer (`PowerDown.Cli`)

**Purpose:** Command-line interface and user interaction

**Components:**
- `Program.cs` - Entry point, DI setup, command-line parsing

**Key Design:** Thin layer that wires up dependencies and delegates to core logic.

## Key Design Patterns

### Interface Segregation Principle

Each interface has a single, focused responsibility:

- `IDownloadDetector` - Only cares about detecting downloads
- `IShutdownService` - Only cares about system shutdown
- `IPlatformDetector` - Only cares about platform detection

**Benefit:** Easy to mock for testing, easy to implement new features without affecting existing code.

### Dependency Injection

All dependencies are injected via constructor:

```csharp
public DownloadOrchestrator(
    IEnumerable<IDownloadDetector> detectors,
    IShutdownService shutdownService,
    ConsoleLogger logger,
    Configuration config)
{
    // ...
}
```

**Benefit:** Testable, loosely coupled, easy to replace implementations.

### Factory Pattern

Path detectors use factory-like static methods:

```csharp
var steamPath = SteamPathDetector.DetectSteamPath(customPath);
var epicPath = EpicPathDetector.DetectEpicPath(customPath);
```

**Benefit:** Encapsulates complex path detection logic, easy to call.

### Strategy Pattern

Different launchers use different detection strategies:

- Steam: Log parsing + VDF file parsing + directory monitoring
- Epic: JSON manifest parsing + LauncherInstalled.dat parsing

**Benefit:** Each launcher can use its own optimal detection approach.

### Composite Pattern

`CompositeDetector` aggregates multiple detectors:

```csharp
public class CompositeDetector : IDownloadDetector
{
    private readonly IEnumerable<IDownloadDetector> _detectors;
    // ...
}
```

**Benefit:** Orchestrator can treat multiple detectors as a single unit.

## Data Flow

### Startup Flow

```
1. User runs: PowerDown.exe --delay 120
   ↓
2. Program.Main() parses CLI arguments
   ↓
3. BuildServiceProvider() configures DI container
   ↓
4. BuildRootCommand() sets up System.CommandLine
   ↓
5. HandleCommandAsync() validates arguments and creates Configuration
   ↓
6. DI container provides DownloadOrchestrator
   ↓
7. DownloadOrchestrator.MonitorAndShutdownAsync() called
```

### Detection Flow

```
1. DownloadOrchestrator.InitializeDetectorsAsync()
   ↓
2. Each IDownloadDetector.InitializeAsync() called
   ↓
3. Steam: Reads Steam path from registry
   ↓
4. Epic: Parses LauncherInstalled.dat
   ↓
5. Detectors ready to monitor
```

### Monitoring Flow

```
1. DownloadOrchestrator.WaitForDownloadsToStartAsync()
   ↓
2. Polls detectors every 5 seconds
   ↓
3. When any detector reports active downloads:
   ↓
4. DownloadOrchestrator.WaitForAllDownloadsToCompleteAsync()
   ↓
5. Polls detectors every 10 seconds (configurable)
   ↓
6. Display active downloads with progress
   ↓
7. When all downloads complete:
   ↓
8. DownloadOrchestrator.RunVerificationPollingAsync()
```

### Verification Flow

```
1. Set polling interval: 10 seconds (configurable)
2. Set total delay: 60 seconds (configurable)
3. Set required checks: 3 consecutive (configurable)
   ↓
4. Loop:
   a. Wait for polling interval
   b. Poll all detectors
   c. If any activity detected:
      - Reset counter to 0
      - Go back to monitoring
   d. If no activity:
      - Increment counter
      - If counter >= required checks:
         - Verification complete
         - Schedule shutdown
```

### Shutdown Flow

```
1. DownloadOrchestrator.ScheduleShutdownAsync()
   ↓
2. Check DryRun mode
   ↓
3. If DryRun: Log "Would shutdown now"
   ↓
4. If not DryRun:
   a. Log "Initiating shutdown in 30 seconds"
   b. Wait 30 seconds for CTRL+C cancellation
   c. IShutdownService.ScheduleShutdownAsync(30, "PowerDown: All downloads complete")
   ↓
5. WindowsShutdownService executes: shutdown.exe /s /t 30 /c "message"
```

## Component Interactions

### DownloadOrchestrator ↔ IDownloadDetector

**Initiated by:** DownloadOrchestrator

**Methods called:**
- `InitializeAsync()` - One-time setup
- `GetActiveDownloadsAsync()` - Get current download list
- `IsAnyDownloadOrInstallActiveAsync()` - Quick status check

**Data exchanged:** `IEnumerable<GameDownloadInfo>`

### DownloadOrchestrator ↔ IShutdownService

**Initiated by:** DownloadOrchestrator

**Methods called:**
- `ScheduleShutdownAsync(delay, message)` - Schedule shutdown
- `CancelShutdownAsync()` - Cancel scheduled shutdown
- `IsShutdownScheduled` - Check if shutdown is scheduled

**Property checked:** `IsShutdownScheduled`

### Path Detectors (SteamPathDetector, EpicPathDetector)

**Responsibility:** Auto-detect launcher installation paths

**Detection order:**
1. Custom path (if provided)
2. Registry/JSON-based detection
3. Default path fallback

**Returns:** `string?` (path or null if not found)

### Download Detectors (SteamDownloadDetector, EpicDownloadDetector)

**Responsibility:** Detect and track download/installation progress

**Steam detection methods:**
- Parse `content_log.txt` for download events
- Parse `appmanifest_*.acf` files for state
- Monitor `steamapps\downloading\` directory

**Epic detection methods:**
- Parse `LauncherInstalled.dat` for installed games
- Parse manifest JSON files for download/installation status

## Technology Choices

### .NET 8 LTS

**Why:**
- Cross-platform support (Windows, Linux, macOS)
- Modern C# 12 features
- Long-term support until November 2026
- Superior performance over .NET Framework

**Alternative considered:** .NET Framework
**Rejected:** Windows-only, no cross-platform support

### System.CommandLine

**Why:**
- Official Microsoft library
- Professional argument parsing
- Auto-generated help messages
- Strong typing for arguments

**Alternative considered:** Manual string parsing
**Rejected:** Error-prone, poor UX, no auto-help

### Dependency Injection (Microsoft.Extensions.DependencyInjection)

**Why:**
- Official Microsoft DI container
- Easy to use
- Excellent test support
- Industry standard for .NET

**Alternative considered:** Manual instantiation
**Rejected:** Harder to test, tightly coupled

### FileSystemWatcher

**Why:**
- Efficient real-time file monitoring
- Built into .NET
- Event-driven (no constant polling)

**Alternative considered:** Constant polling
**Rejected:** More resource-intensive, slower detection

## Extension Points

### Adding a New Platform

Create new project: `PowerDown.Platform.Linux`

1. Implement `IPlatformDetector` - Detect Linux OS
2. Implement `IShutdownService` - Linux shutdown command
3. Implement `IDownloadDetector` for each launcher on Linux
4. Update DI setup in `Program.cs`

### Adding a New Launcher

Create new detector class implementing `IDownloadDetector`:

1. Implement `LauncherName` property
2. Implement `InitializeAsync()` - Setup detection
3. Implement `GetActiveDownloadsAsync()` - Return current downloads
4. Implement `IsAnyDownloadOrInstallActiveAsync()` - Quick check

Register in DI container in `Program.cs`

## Security Considerations

### Shutdown Scheduling

- Uses Windows `shutdown.exe` command
- Requires user has shutdown privileges
- Does not elevate privileges (user must have rights)

### Path Detection

- Only reads from registry and file system
- Does not modify registry or files
- Falls back to defaults if detection fails

### File Monitoring

- Reads log files, does not modify them
- Uses `FileSystemWatcher` for efficient monitoring
- Handles file access errors gracefully

## Performance Considerations

### Polling Intervals

- Default: 10 seconds during monitoring
- Configurable via `--interval` flag
- Balance between responsiveness and resource usage

### Verification Period

- Default: 60 seconds total
- Default: 3 consecutive checks at 10-second intervals
- Configurable via `--delay` and `--checks` flags
- Prevents premature shutdown on resumed downloads

### Memory Usage

- Minimal: Only tracks active downloads in memory
- No large file buffering (stream-based log reading)
- Disposes resources properly

## Error Handling

### Graceful Degradation

- If one detector fails, others continue working
- Logs warnings but doesn't crash
- Uses defaults if path detection fails

### User-Friendly Errors

- Clear error messages for common issues
- Suggests solutions (e.g., "Check your installation paths")
- Verbose mode for debugging (`--verbose`)

### Cancellation Handling

- CTRL+C gracefully cancels shutdown
- Cancels scheduled shutdowns during verification
- Cleans up resources properly

## Future Architecture Improvements

### Configuration Files

Add `appsettings.json` support for persistent configuration.

### Plugin System

Add plugin architecture for third-party launcher detectors.

### Service Architecture

Add Windows service mode for background operation.

### Remote Control

Add HTTP API for remote monitoring and control.
