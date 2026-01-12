# Development Guide

This document provides guidelines for developing PowerDown, including setup, coding standards, and contribution process.

## Prerequisites

### Required Software

- **.NET 8 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **Git** - Version control
- **IDE** - Visual Studio 2022+, Visual Studio Code, or Rider

### Optional Software

- **Visual Studio 2022** - Recommended for Windows development
- **Visual Studio Code** - Lightweight alternative with C# extension
- **JetBrains Rider** - Alternative IDE with .NET support

## Setup

### 1. Clone Repository

```bash
git clone https://github.com/yourusername/PowerDown.git
cd PowerDown
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build Solution

```bash
dotnet build
```

### 4. Run Tests

```bash
dotnet test
```

### 5. Run Application

```bash
cd src/PowerDown.Cli
dotnet run
```

## Project Structure

```
PowerDown/
├── src/
│   ├── PowerDown.Abstractions/          # Platform-agnostic interfaces
│   ├── PowerDown.Core/                # Business logic
│   ├── PowerDown.Platform.Windows/       # Windows implementations
│   └── PowerDown.Cli/                # CLI application
├── tests/
│   ├── PowerDown.Core.Tests/
│   ├── PowerDown.Platform.Windows.Tests/
│   ├── PowerDown.Cli.Tests/
│   └── PowerDown.IntegrationTests/
└── docs/
```

## Coding Standards

### C# Style

PowerDown follows these conventions:

1. **PascalCase for all public members** - Methods, properties, classes
2. **CamelCase for parameters and local variables**
3. **Async methods end with `Async` suffix**
4. **Interfaces start with `I` prefix** - e.g., `IDownloadDetector`
5. **Private fields use `_camelCase` prefix** - e.g., `_logger`

### Example

```csharp
public class ExampleService
{
    private readonly ILogger _logger;
    private readonly string _basePath;

    public ExampleService(ILogger logger, string basePath)
    {
        _logger = logger;
        _basePath = basePath;
    }

    public async Task<string> GetDataAsync()
    {
        return await _logger.LogAsync(_basePath);
    }

    private string ProcessData(string data)
    {
        return data.ToUpper();
    }
}
```

### Code Comments

Every class file should have a comment at the top describing its purpose:

```csharp
// Coordinates multiple download detectors and manages verification polling
// Handles the main orchestration logic for monitoring downloads
public class DownloadOrchestrator
{
    // ...
}
```

### File Naming

- **Source files:** `PascalCase.cs` - e.g., `DownloadOrchestrator.cs`
- **Test files:** `*Tests.cs` - e.g., `DownloadOrchestratorTests.cs`
- **Test data files:** `snake_case.txt` or `PascalCase.acf`

### Naming Conventions

| Type | Convention | Example |
|-------|-------------|----------|
| Classes | PascalCase | `DownloadOrchestrator` |
| Interfaces | IPascalCase | `IDownloadDetector` |
| Methods | PascalCase | `GetActiveDownloadsAsync()` |
| Properties | PascalCase | `LauncherName` |
| Fields | _camelCase | `_logger` |
| Constants | PascalCase | `MaxRetries` |
| Enums | PascalCase | `DownloadStatus` |

## Testing

### Running Tests

Run all tests:

```bash
dotnet test
```

Run specific test project:

```bash
dotnet test PowerDown.Core.Tests
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Run specific test:

```bash
dotnet test --filter "FullyQualifiedName~DownloadOrchestratorTests"
```

### Writing Tests

Follow these guidelines:

1. **Arrange-Act-Assert pattern**
2. **Descriptive test method names** - TestName_ExpectedBehavior()
3. **Use `[Fact]` for single scenarios**
4. **Use `[Theory]` for multiple inputs**
5. **Mock external dependencies** using Moq
6. **Use FluentAssertions** for readable assertions

### Example Test

```csharp
[Fact]
public async Task DownloadOrchestrator_WithDryRun_DoesNotCallShutdown()
{
    // Arrange
    var mockDetector = new Mock<IDownloadDetector>();
    var mockShutdown = new Mock<IShutdownService>();
    var config = new Configuration { DryRun = true };

    var orchestrator = new DownloadOrchestrator(
        new[] { mockDetector.Object },
        mockShutdown.Object,
        new ConsoleLogger(),
        config);

    // Act
    await orchestrator.MonitorAndShutdownAsync();

    // Assert
    mockShutdown.Verify(s => s.ScheduleShutdownAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
}
```

### Coverage Goals

- **Core logic:** 90%+ coverage
- **Platform logic:** 80%+ coverage
- **Detection logic:** 70%+ coverage
- **CLI:** 85%+ coverage
- **Overall:** 75%+ coverage

## Adding Features

### Adding a New Platform

See [docs/API.md](API.md#adding-a-new-platform) for detailed guide.

**Summary:**
1. Create `PowerDown.Platform.NewPlatform` project
2. Implement `IPlatformDetector`
3. Implement `IShutdownService`
4. Implement `IDownloadDetector` for each launcher
5. Update DI setup in `PowerDown.Cli/Program.cs`
6. Write tests in `PowerDown.Platform.NewPlatform.Tests`

### Adding a New Launcher

See [docs/API.md](API.md#adding-a-new-launcher) for detailed guide.

**Summary:**
1. Create detector class implementing `IDownloadDetector`
2. Create path detector if needed
3. Implement all required methods
4. Register in DI container
5. Write tests

### Adding CLI Arguments

1. Add option in `BuildRootCommand()` method:

```csharp
var newOption = new Option<bool>(
    ["--new-option"],
    description: "Description of option");

rootCommand.AddOption(newOption);
```

2. Add parameter to handler:

```csharp
rootCommand.SetHandler(async (newOption, ...) =>
{
    return await HandleCommandAsync(..., newOption);
},
newOption,
...);
```

3. Update `Configuration` class:

```csharp
public class Configuration
{
    public bool NewOption { get; set; }
}
```

4. Use in logic:

```csharp
if (config.NewOption)
{
    // Handle new option
}
```

## Debugging

### Visual Studio

1. Set breakpoints in code
2. Press F5 to start debugging
3. Use Debug menu for stepping

### Visual Studio Code

1. Install C# extension
2. Set `launch.json` configuration
3. Press F5 to start debugging

### Command-Line Debugging

Run with verbose logging:

```bash
dotnet run -- --verbose
```

Attach debugger to running process:

```bash
dotnet run
# In separate terminal
dotnet attach <process-id>
```

## Git Workflow

### Branch Strategy

- `main` - Production code
- `develop` - Integration branch for features
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches

### Commit Messages

Follow conventional commits format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation changes
- `refactor` - Code refactoring
- `test` - Adding or updating tests
- `chore` - Maintenance tasks

**Examples:**

```
feat(steam): Add Steam download detection via log parsing

Parse content_log.txt to detect download start, progress, and completion events.
```

```
fix(epic): Handle missing LauncherInstalled.dat gracefully

Added fallback to default path when Epic manifest file is not found.
```

### Pull Request Process

1. Create feature branch from `develop`
2. Make changes with clear commit messages
3. Write tests for new functionality
4. Ensure all tests pass
5. Create pull request to `develop`
6. Request code review
7. Address review feedback
8. Merge after approval

## Code Review Checklist

Before submitting a pull request, ensure:

- [ ] All tests pass
- [ ] Code follows naming conventions
- [ ] Public APIs have XML documentation comments
- [ ] Tests cover new functionality
- [ ] No compiler warnings
- [ ] No debug or console output remains
- [ ] Error handling is appropriate
- [ ] Dependencies are minimal and necessary

## Release Process

### Version Bump

Update version in all `.csproj` files:

```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

### Build Release

```bash
dotnet build -c Release
```

### Run Tests

```bash
dotnet test -c Release
```

### Create Release Package

```bash
dotnet publish -c Release -o publish
```

### Tag Release

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Create GitHub Release

1. Go to GitHub Releases
2. Create new release with tag `v1.0.0`
3. Add release notes
4. Attach build artifacts

## Documentation

### Updating Documentation

When making changes:

1. Update relevant documentation file
2. Update API documentation if changing public interfaces
3. Add ADR for architectural changes
4. Update README for user-facing changes
5. Update CHANGELOG if tracking

### Documentation Format

- Use Markdown (`.md`) files
- Use consistent heading levels (`#`, `##`, `###`)
- Include code examples with language tags
- Cross-link related documents

## Performance Guidelines

### Async/Await

- Always use `async`/`await` for I/O operations
- Avoid `.Result` or `.Wait()` - causes deadlocks
- Use `ConfigureAwait(false)` in library code

### Memory

- Dispose of `IDisposable` objects properly
- Use `using` statements for resource cleanup
- Avoid large in-memory buffers (use streams)

### CPU Usage

- Use `FileSystemWatcher` instead of constant polling
- Poll only at necessary intervals (default: 10 seconds)
- Avoid tight loops with `Thread.Sleep()`

## Security Guidelines

### User Input

- Validate all command-line arguments
- Sanitize file paths
- Handle null or empty strings gracefully

### External Dependencies

- Use NuGet packages from trusted sources
- Review package updates for security issues
- Keep dependencies up to date

### File System

- Don't modify launcher files
- Handle file access errors gracefully
- Don't log sensitive information

## Getting Help

### Resources

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)

### Asking Questions

1. Check existing documentation
2. Search GitHub issues
3. Ask in GitHub Discussions
4. Create issue for bugs or feature requests

## Checklist

Before contributing:

- [ ] Code follows coding standards
- [ ] All tests pass
- [ ] New features have tests
- [ ] Documentation updated
- [ ] Commit messages follow format
- [ ] PR description explains changes
- [ ] No compiler warnings
- [ ] Code reviewed (if possible)

Thank you for contributing to PowerDown!
