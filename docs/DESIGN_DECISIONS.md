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
Many games have separate download and installation phases. Shutdown must wait for both to complete.

**Decision:**
Track `DownloadStatus` and `InstallStatus` separately in `GameDownloadInfo`. Both must be `Idle` for game to be considered complete.

**Rationale:**
- **Accurate completion:** Game is only truly ready when installed
- **User expectation:** Users expect game to be fully installed, not just downloaded
- **Better UX:** Shows both statuses separately (e.g., "Downloading" vs "Installing")

**Consequences:**

**Positive:**
- More accurate completion detection
- Better user experience with detailed status
- Prevents premature shutdown during installation
- Handles games with long installation times

**Negative:**
- Increased complexity in status tracking
- More complex verification logic
- Need to detect both states for each launcher

---

## ADR-006: Use Avalonia for Cross-Platform UI

**Status:** Accepted

**Context:**
The application targets gamers on Windows, Linux, and macOS and needs a consistent desktop UI experience.

**Decision:**
Use Avalonia for the cross-platform desktop UI.

**Rationale:**
- **Cross-platform:** Single UI stack for Windows, Linux, and macOS
- **XAML-based:** Familiar layout and styling workflow
- **Active community:** Well-supported open source project
- **Theming:** Enables consistent branding and modern visuals

**Consequences:**

**Positive:**
- Consistent UI across platforms
- Fast iteration with XAML and styles
- Easier onboarding for non-technical users
- Simplifies future UI enhancements

**Negative:**
- External dependency
- Requires UI testing and visual validation

---

## ADR-007: Use FileSystemWatcher for Real-Time Monitoring

**Status:** Accepted

**Context:**
The application needs to detect log file changes for real-time download monitoring. Constant polling of files is inefficient.

**Decision:**
Use `FileSystemWatcher` to monitor Steam log/manifest files for changes, with polling fallback for status checks.

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

## ADR-009: Log Levels for User-Facing Status

**Status:** Accepted

**Context:**
The application needs to communicate status to users clearly. Different message types (info, warning, error) should be visually distinct.

**Decision:**
Implement `ILogger` with log levels (Info, Warning, Error, Verbose, Success) and surface them through the UI.

**Rationale:**
- **Better UX:** Visual distinction helps users quickly identify important messages
- **Configurable:** Verbose logging optional
- **Built-in:** Uses .NET's `Console` class, no dependencies

**Consequences:**

**Positive:**
- Clear, readable status messaging
- Easy to identify warnings and errors
- Optional verbose mode for debugging
- No external dependencies

**Negative:**
- Requires UI wiring for message display
- Adds logging class to maintain

---

## ADR-010: Steam Log Parsing for Download Detection

**Status:** Accepted

**Context:**
Steam writes download events to `content_log.txt`. The application needs to parse these logs to detect download activity and completion.

**Decision:**
Parse Steam's `content_log.txt` file using regex patterns to match download events (Starting, Downloading, Download complete, Installed).

**Rationale:**
- **Official source:** Steam's own log file for download events
- **Real-time:** Log file is updated continuously during downloads
- **Rich information:** Contains game names, sizes, and completion events
- **No API needed:** Doesn't require Steam API integration

**Consequences:**

**Positive:**
- Detects download activity in real-time
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
The application has multiple concerns: abstractions, core logic, platform code, UI, and tests. Clear organization is essential for maintainability.

**Decision:**
Organize solution into distinct projects: `PowerDown.Abstractions`, `PowerDown.Core`, `PowerDown.Platform.Windows`, `PowerDown.Platform.Linux`, `PowerDown.Platform.macOS`, `PowerDown.UI`, plus separate test projects for each.

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

## ADR-013: Configuration via Settings File and UI

**Status:** Accepted

**Context:**
The application needs to be configurable. Options include in-app settings, configuration files, and environment variables.

**Decision:**
Use `appsettings.json` for persisted defaults and expose key settings through the UI.

**Rationale:**
- **User-friendly:** Settings are visible and adjustable without typing commands
- **Persisted:** Defaults survive restarts
- **Explicit:** Settings are grouped and described in the UI

**Consequences:**

**Positive:**
- Simple to adjust without CLI knowledge
- Settings persist across sessions
- Easier onboarding for non-technical users

**Negative:**
- Requires validating user input from the UI
- Requires schema updates when adding new settings

---

## ADR-014: Graceful Shutdown Cancellation via UI

**Status:** Accepted

**Context:**
Users may want to cancel the scheduled shutdown before it executes.

**Decision:**
Expose a UI command to cancel scheduled shutdown using the platform shutdown service, and clean up resources.

**Rationale:**
- **User control:** Users can cancel shutdown during the countdown
- **Graceful cleanup:** Properly disposes resources and stops polling

**Consequences:**

**Positive:**
- User can cancel shutdown
- Proper cleanup of resources
- No orphaned shutdowns
- Matches user expectations for desktop apps

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
Create separate test projects for each source project: `PowerDown.Core.Tests`, `PowerDown.Platform.Windows.Tests`, `PowerDown.Platform.Linux.Tests`, `PowerDown.Platform.macOS.Tests`, and `PowerDown.UI.Tests`.

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
- Linux and macOS platform implementations
- Additional launcher integrations
- Plugin system architecture
- Remote control API design
