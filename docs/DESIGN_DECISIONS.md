# Design Decisions

This document records architectural decisions made during PowerDown development using ADR (Architecture Decision Record) format.

---

## ADR-001: Choose .NET 8 over .NET Framework

**Status:** Accepted

**Context:**
PowerDown needs to support multiple platforms (Windows initially, Linux and macOS planned). The application will be long-term maintained and should use modern language features.

**Decision:**
Use .NET 8 LTS as the target framework.

**Rationale:**
- **Cross-platform:** .NET 8 runs on Windows, Linux, and macOS, enabling future platform expansion
- **Modern features:** C# 12 language features and latest .NET APIs
- **Long-term support:** LTS until November 2026, providing stability
- **Performance:** Significantly faster than .NET Framework
- **Community:** Large ecosystem and active development

**Consequences:**

**Positive:**
- Easy to add Linux and macOS platforms later
- Access to modern .NET APIs and libraries
- Better performance and smaller deployment size
- Strong community support and documentation

**Negative:**
- Requires .NET 8 runtime (but included in Windows 11+)
- Slight learning curve for developers used to .NET Framework

---

## ADR-002: Separate Abstractions Layer

**Status:** Accepted

**Context:**
The application needs to support multiple platforms (Windows, Linux, macOS) and potentially multiple game launchers. Platform-specific code must be isolated from core business logic.

**Decision:**
Create `PowerDown.Abstractions` project containing platform-agnostic interfaces and models.

**Rationale:**
- **Dependency inversion:** Core logic depends on abstractions, not implementations
- **Easy testing:** Can mock interfaces without concrete implementations
- **Future expansion:** Add new platforms by implementing interfaces, not modifying core logic
- **Clean separation:** Clear boundary between platform code and business logic

**Consequences:**

**Positive:**
- Easy to add new platforms without touching existing code
- High testability with mocking frameworks
- Clear architectural boundaries
- Reusable abstractions for different platforms

**Negative:**
- Additional project and file structure complexity
- More boilerplate code for interface implementations

---

## ADR-003: Use Registry for Steam Path Detection

**Status:** Accepted

**Context:**
Steam can be installed in non-default locations. The application needs to reliably detect Steam installation path without user configuration.

**Decision:**
Read Steam path from Windows registry at `HKCU\Software\Valve\Steam\SteamPath`, with fallback to default path.

**Rationale:**
- **Official location:** Steam stores its path in this registry key
- **Reliable:** Works even when Steam is installed in custom locations
- **User-friendly:** Most users don't need manual configuration
- **Fallback:** Default path as backup if registry is unavailable

**Consequences:**

**Positive:**
- Automatic detection for most users
- Works with custom Steam installations
- Fallback to default path ensures functionality

**Negative:**
- Registry access requires appropriate permissions (typically available)
- Fails if Steam was installed but registry is corrupted
- Windows-specific approach (Linux will need different method)

---

## ADR-004: Polling-Based Verification

**Status:** Accepted

**Context:**
Game installations can take longer than downloads. A simple delay after download completion may shutdown the PC during installation. Downloads can also resume during this period.

**Decision:**
Implement polling-based verification with configurable total delay and required consecutive idle checks (default: 60 seconds, 3 checks).

**Rationale:**
- **Handles installation time:** Polling continues during installation
- **Detects resumed downloads:** If a new download starts, verification resets
- **Reduces false positives:** Multiple consecutive checks ensure stability
- **Configurable:** Users can adjust based on their needs

**Consequences:**

**Positive:**
- Prevents shutdown during installations
- Detects and handles resumed downloads
- Configurable for different use cases
- More reliable than simple delay

**Negative:**
- Adds complexity to orchestrator logic
- Longer total time before shutdown (but safer)
- Requires tuning of default values

---

## ADR-005: Separate Download and Install Detection

**Status:** Accepted

**Context:**
Many games, especially on Epic Games, have separate download and installation phases. Shutdown must wait for both to complete.

**Decision:**
Track `DownloadStatus` and `InstallStatus` separately in `GameDownloadInfo`. Both must be `Idle` for game to be considered complete.

**Rationale:**
- **Accurate completion:** Game is only truly ready when installed
- **User expectation:** Users expect game to be fully installed, not just downloaded
- **Better UX:** Shows both statuses separately (e.g., "Downloading 50%" vs "Installing 95%")

**Consequences:**

**Positive:**
- More accurate completion detection
- Better user experience with detailed progress
- Prevents premature shutdown during installation
- Handles games with long installation times

**Negative:**
- Increased complexity in status tracking
- More complex verification logic
- Need to detect both states for each launcher

---

## ADR-006: Use System.CommandLine for CLI Parsing

**Status:** Accepted

**Context:**
The application requires flexible command-line argument parsing with validation, help generation, and multiple options.

**Decision:**
Use `System.CommandLine` (Microsoft's official CLI library) for argument parsing.

**Rationale:**
- **Official library:** Maintained by Microsoft, well-documented
- **Type safety:** Strongly typed argument values
- **Auto-help:** Automatic generation of help messages
- **Validation:** Built-in validation for argument types
- **Industry standard:** Used by many .NET CLI tools

**Consequences:**

**Positive:**
- Professional CLI experience
- Auto-generated help messages
- Type-safe argument parsing
- Less boilerplate code
- Easy to add new arguments

**Negative:**
- External dependency
- Learning curve for developers new to the library
- Beta version (as of development time)

---

## ADR-007: Use FileSystemWatcher for Real-Time Monitoring

**Status:** Accepted

**Context:**
The application needs to detect log file changes for real-time download monitoring. Constant polling of files is inefficient.

**Decision:**
Use `FileSystemWatcher` to monitor Steam and Epic log/manifest files for changes, with polling fallback for status checks.

**Rationale:**
- **Efficient:** Event-driven, no constant polling
- **Responsive:** Immediate notification of file changes
- **Built-in:** Part of .NET, no external dependencies
- **Low resource usage:** Doesn't consume CPU when idle

**Consequences:**

**Positive:**
- Real-time detection of download events
- Low CPU and memory usage
- Responsive user experience
- Simplifies log file reading (only read new content)

**Negative:**
- Doesn't work on all file systems (but works on Windows NTFS)
- File system events can be unreliable in some scenarios
- Requires fallback to polling for status checks

---

## ADR-008: Composite Detector Pattern

**Status:** Accepted

**Context:**
The application may need to monitor multiple game launchers simultaneously. Orchestrator should treat them as a single unit.

**Decision:**
Implement `CompositeDetector` class that aggregates multiple `IDownloadDetector` instances, providing a single interface to the orchestrator.

**Rationale:**
- **Simplifies orchestrator:** Works with one detector regardless of actual count
- **Flexible:** Easy to add or remove detectors
- **Single responsibility:** Orchestrator doesn't need to know about specific detectors
- **Clean architecture:** Fits composite design pattern

**Consequences:**

**Positive:**
- Simplifies orchestrator logic
- Easy to add new detectors
- Clean separation of concerns
- Matches standard design patterns

**Negative:**
- Additional abstraction layer
- Small performance overhead (negligible)
- Composite class adds complexity

---

## ADR-009: Colored Console Output with Log Levels

**Status:** Accepted

**Context:**
The application needs to communicate status to users clearly. Different message types (info, warning, error) should be visually distinct.

**Decision:**
Implement `ConsoleLogger` with colored output using `Console.ForegroundColor` and log levels (Info, Warning, Error, Verbose, Success).

**Rationale:**
- **Better UX:** Visual distinction helps users quickly identify important messages
- **Professional:** Matches CLI tools' best practices
- **Configurable:** Verbose logging optional
- **Built-in:** Uses .NET's `Console` class, no dependencies

**Consequences:**

**Positive:**
- Clear, readable console output
- Easy to identify warnings and errors
- Optional verbose mode for debugging
- No external dependencies

**Negative:**
- Color codes may not work in all terminal emulators
- Windows Terminal required for best experience
- Adds logging class to maintain

---

## ADR-010: Epic Path Detection via LauncherInstalled.dat

**Status:** Accepted

**Context:**
Epic Games Launcher stores installation information. The application needs to detect Epic Games path without user configuration.

**Decision:**
Parse `LauncherInstalled.dat` JSON file in `%ProgramData%\Epic\UnrealEngineLauncher\` to detect Epic Games path and installed games.

**Rationale:**
- **Official format:** Epic's documented file for installation tracking
- **Reliable:** Maintained by Epic Games Launcher
- **Rich information:** Contains game names, versions, and installation locations
- **Fallback:** Use default path if file is unavailable

**Consequences:**

**Positive:**
- Automatic detection of Epic Games and installed games
- Access to game metadata (names, versions)
- Works with custom Epic installations
- Fallback to default path ensures functionality

**Negative:**
- JSON parsing adds complexity
- File location may vary (but standard location exists)
- Requires JSON deserialization

---

## ADR-011: Steam Log Parsing for Download Detection

**Status:** Accepted

**Context:**
Steam writes download events to `content_log.txt`. The application needs to parse these logs to detect download progress and completion.

**Decision:**
Parse Steam's `content_log.txt` file using regex patterns to match download events (Starting, Downloading, Download complete, Installed).

**Rationale:**
- **Official source:** Steam's own log file for download events
- **Real-time:** Log file is updated continuously during downloads
- **Rich information:** Contains game names, sizes, and completion events
- **No API needed:** Doesn't require Steam API integration

**Consequences:**

**Positive:**
- Detects download progress in real-time
- Shows game names from log messages
- No external API dependencies
- Works with all Steam versions

**Negative:**
- Log file format may change (Steam updates)
- Regex-based parsing can be fragile
- Requires file position tracking to avoid re-reading

---

## ADR-012: Use Directory Structure with Clear Layer Separation

**Status:** Accepted

**Context:**
The application has multiple concerns: abstractions, core logic, platform code, CLI, and tests. Clear organization is essential for maintainability.

**Decision:**
Organize solution into distinct projects: `PowerDown.Abstractions`, `PowerDown.Core`, `PowerDown.Platform.Windows`, `PowerDown.Cli`, plus separate test projects for each.

**Rationale:**
- **Clear boundaries:** Each project has a single, well-defined responsibility
- **Future expansion:** Easy to add new platform projects
- **Test separation:** Tests mirror source structure
- **Dependency graph:** Clear visual dependencies between layers

**Consequences:**

**Positive:**
- Maintainable project structure
- Easy to understand code organization
- Simple to add new platforms
- Tests are clearly separated

**Negative:**
- More projects to manage
- Increased solution file size
- Slightly longer build times (multiple projects)

---

## ADR-013: Configuration via Command-Line Only (v1)

**Status:** Accepted

**Context:**
The application needs to be configurable. Options include command-line arguments, configuration files, and environment variables.

**Decision:**
For v1.0, use only command-line arguments for configuration. Configuration files and environment variables planned for future versions.

**Rationale:**
- **Simplicity:** Faster to implement and test
- **Standard:** Matches CLI tools' typical behavior
- **Explicit:** Users see all options via `--help`
- **No state:** No configuration file corruption or migration issues

**Consequences:**

**Positive:**
- Simple implementation
- No configuration file management
- Clear user expectations
- Easy to test

**Negative:**
- Must retype arguments every run
- Can't save favorite configurations
- Longer command lines for complex setups
- Less convenient for power users

**Future improvement:** Add `appsettings.json` support in v1.1

---

## ADR-014: Graceful Shutdown Handling with CTRL+C

**Status:** Accepted

**Context:**
Users may want to cancel the scheduled shutdown before it executes. Standard Windows shutdown commands don't provide easy cancellation.

**Decision:**
Handle `Console.CancelKeyPress` event to detect CTRL+C, cancel scheduled shutdown using `shutdown.exe /a`, and clean up resources.

**Rationale:**
- **User control:** Users can cancel shutdown during 30-second countdown
- **Graceful cleanup:** Properly disposes resources and stops polling
- **Windows standard:** Uses standard `shutdown.exe /a` command
- **Expected behavior:** Matches user expectations for CLI tools

**Consequences:**

**Positive:**
- User can cancel shutdown
- Proper cleanup of resources
- No orphaned shutdowns
- Matches CLI best practices

**Negative:**
- Adds complexity to shutdown flow
- Requires managing cancellation tokens
- Only works during countdown period (before Windows takes over)

---

## ADR-015: Separate Project Structure for Tests

**Status:** Accepted

**Context:**
The application needs comprehensive testing. Different types of tests (unit, integration, E2E) have different requirements and should be clearly separated.

**Decision:**
Create separate test projects for each source project: `PowerDown.Core.Tests`, `PowerDown.Platform.Windows.Tests`, `PowerDown.Cli.Tests`, and `PowerDown.IntegrationTests`.

**Rationale:**
- **Clear test organization:** Tests mirror source structure
- **Test data isolation:** Each project can have its own test data
- **Dependency separation:** Unit tests don't depend on integration tests
- **CI/CD friendly:** Easy to run specific test suites

**Consequences:**

**Positive:**
- Clear test organization
- Easy to find and run specific tests
- Test data isolated by project
- Simple CI/CD configuration

**Negative:**
- More test projects to maintain
- Longer build time for tests
- Potential duplication of test utilities

---

## Future ADRs

As the project evolves, new ADRs will be added for:
- Configuration file support
- GUI application architecture
- Linux and macOS platform implementations
- Additional launcher integrations
- Plugin system architecture
- Remote control API design
