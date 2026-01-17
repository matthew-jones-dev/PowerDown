# Testing Guide

This document describes PowerDown's testing strategy, how to run tests, and guidelines for writing tests.

## Testing Strategy

PowerDown uses a multi-layered testing approach:

### Unit Tests (70% of tests)

Test individual components in isolation with mocked dependencies.

**Coverage goals:**
- Core logic: 90%+
- Platform logic: 80%+
- Detection logic: 70%+
- UI: 80%+

**Examples:**
- `ConfigurationTests` - Test configuration model
- `ConsoleLoggerTests` - Test logging functionality
- `SteamPathDetectorTests` - Test path detection logic

### Integration Tests (20% of tests)

Test interactions between components with some real dependencies.

**Examples:**
- `DownloadOrchestratorTests` - Test orchestrator with mock detectors
- `MainViewModelTests` - Test UI orchestration and state

### Near-E2E Tests (10% of tests)

Test complete workflows with mocked external systems (Steam processes, file system, registry).

**Examples:**
- `MonitoringWorkflowTests` - Test complete monitoring flow
- `DownloadResumeTests` - Test verification with resumed downloads
- `MultipleLaunchersTests` - Test multiple launcher scenarios

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project

```bash
dotnet test PowerDown.Core.Tests
dotnet test PowerDown.Platform.Windows.Tests
dotnet test PowerDown.Platform.Linux.Tests
dotnet test PowerDown.Platform.macOS.Tests
dotnet test PowerDown.UI.Tests
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~ConfigurationTests"
```

### Run Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~ConfigurationTests_DefaultValues_AreCorrect"
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports are generated in `TestResults` directory.

### Run Tests in Debug Mode

```bash
dotnet test -c Debug
```

### Run Tests in Release Mode

```bash
dotnet test -c Release
```

## Test Data

### Location

Test data files are located in `tests/PowerDown.Platform.Windows.Tests/TestData/`:

```
TestData/
├── Steam/
│   ├── content_log_sample.txt
│   ├── content_log_empty.txt
│   ├── content_log_complete.txt
│   ├── libraryfolders.vdf
│   └── appmanifest_570.acf
```

### Purpose

- **Steam logs** - Sample Steam content_log.txt with various download states
- **Steam VDF** - Sample Valve Data Format files (appmanifest, libraryfolders)

### Adding Test Data

When adding new test data:

1. Use realistic samples from actual installations
2. Sanitize sensitive information (personal paths, game IDs)
3. Document the purpose in the filename or comment
4. Update test to use the new data

## Writing Tests

### Test Structure

Follow this pattern:

```csharp
using Xunit;
using FluentAssertions;
using Moq;
using PowerDown.SomeNamespace;

namespace PowerDown.SomeNamespace.Tests;

public class SomeClassTests
{
    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var mockService = new Mock<IService>();
        mockService.Setup(s => s.SomeMethod()).ReturnsAsync(expectedValue);
        
        var sut = new SomeClass(mockService.Object);
        
        // Act
        var result = await sut.MethodNameAsync(input);
        
        // Assert
        result.Should().Be(expectedValue);
        mockService.Verify(s => s.SomeMethod(), Times.Once);
    }
}
```

### Unit Tests

**Guidelines:**

1. **Test one thing per test**
2. **Arrange-Act-Assert pattern**
3. **Mock all external dependencies**
4. **Use descriptive test names**
5. **Test both success and failure paths**

**Example:**

```csharp
[Fact]
public void Configuration_DefaultValues_AreCorrect()
{
    var config = new Configuration();
    
    config.VerificationDelaySeconds.Should().Be(120);
    config.PollingIntervalSeconds.Should().Be(15);
    config.RequiredNoActivityChecks.Should().Be(5);
    config.ShutdownDelaySeconds.Should().Be(60);
}
```

### Parameterized Tests

Use `[Theory]` and `[InlineData]` for testing multiple inputs:

```csharp
[Theory]
[InlineData(30)]
[InlineData(60)]
[InlineData(120)]
public void Configuration_VerificationDelaySeconds_CanBeSet(int delay)
{
    var config = new Configuration { VerificationDelaySeconds = delay };
    config.VerificationDelaySeconds.Should().Be(delay);
}
```

### Async Tests

For async methods:

```csharp
[Fact]
public async Task DownloadOrchestrator_InitializesDetectors_Async()
{
    var mockDetector = new Mock<IDownloadDetector>();
    mockDetector.Setup(d => d.InitializeAsync()).ReturnsAsync(true);
    
    var orchestrator = new DownloadOrchestrator(
        new[] { mockDetector.Object },
        Mock.Of<IShutdownService>(),
        Mock.Of<ConsoleLogger>(),
        new Configuration());
    
    await orchestrator.MonitorAndShutdownAsync();
    
    mockDetector.Verify(d => d.InitializeAsync(), Times.Once);
}
```

### Exception Tests

Test that appropriate exceptions are thrown:

```csharp
[Fact]
public void Constructor_WithNullParameter_ThrowsArgumentNullException()
{
    Action act = () => new SomeClass(null!);
    
    act.Should().Throw<ArgumentNullException>()
        .And.ParamName.Should().Be("parameter");
}
```

### Integration Tests

Test component interactions with partial real dependencies:

```csharp
[Fact]
public async Task DependencyInjection_RegistersCorrectly()
{
    var services = new ServiceCollection();
    services.AddSingleton<Configuration>();
    services.AddSingleton<ConsoleLogger>();
    
    var provider = services.BuildServiceProvider();
    var config = provider.GetRequiredService<Configuration>();
    
    config.Should().NotBeNull();
}
```

### Near-E2E Tests

Test complete workflows with mocked external systems:

```csharp
[Fact]
public async Task MonitoringWorkflow_NoDownloads_StartsWaitsForDownloads()
{
    var mockDetector = new Mock<IDownloadDetector>();
    mockDetector.Setup(d => d.IsAnyDownloadOrInstallActiveAsync()).ReturnsAsync(false);
    
    var orchestrator = new DownloadOrchestrator(
        new[] { mockDetector.Object },
        Mock.Of<IShutdownService>(),
        Mock.Of<ConsoleLogger>(),
        new Configuration { VerificationDelaySeconds = 0, RequiredNoActivityChecks = 1 });
    
    await orchestrator.MonitorAndShutdownAsync();
    
    mockDetector.Verify(d => d.IsAnyDownloadOrInstallActiveAsync(), Times.AtLeastOnce);
}
```

## Mocking

### Using Moq

**Setup return value:**

```csharp
mockDetector.Setup(d => d.GetActiveDownloadsAsync())
    .ReturnsAsync(new List<GameDownloadInfo>());
```

**Setup to throw exception:**

```csharp
mockDetector.Setup(d => d.InitializeAsync())
    .ThrowsAsync(new DirectoryNotFoundException("Path not found"));
```

**Verify method was called:**

```csharp
mockDetector.Verify(d => d.GetActiveDownloadsAsync(), Times.Once);
mockDetector.Verify(d => d.InitializeAsync(), Times.AtLeastOnce);
```

**Verify method was not called:**

```csharp
mockDetector.Verify(d => d.CancelShutdownAsync(), Times.Never);
```

### Custom Mock Classes

For complex scenarios, create custom mock classes:

```csharp
public class MockDownloadDetector : IDownloadDetector
{
    public string LauncherName => "Mock";
    
    private readonly List<GameDownloadInfo> _downloads;
    private readonly bool _isActive;
    
    public MockDownloadDetector(List<GameDownloadInfo> downloads, bool isActive = false)
    {
        _downloads = downloads;
        _isActive = isActive;
    }

    public Task<IEnumerable<GameDownloadInfo>> GetActiveDownloadsAsync()
    {
        return Task.FromResult<IEnumerable<GameDownloadInfo>>(_downloads);
    }

    public Task<bool> IsAnyDownloadOrInstallActiveAsync()
    {
        return Task.FromResult(_isActive);
    }

    public Task<bool> InitializeAsync()
    {
        return Task.FromResult(true);
    }
}
```

## Coverage

### Coverage Goals

| Component | Target | Current |
|-----------|--------|----------|
| Core Logic | 90%+ | TBD |
| Platform Logic | 80%+ | TBD |
| Detection Logic | 70%+ | TBD |
| UI | 80%+ | TBD |
| Overall | 75%+ | TBD |

### Checking Coverage

Run tests with coverage collection:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Open coverage report:

**Windows:**
```bash
start TestResults\*\coverage\*.html
```

**Linux/macOS:**
```bash
open TestResults/*/coverage/*.html
```

### Improving Coverage

1. Identify uncovered lines in coverage report
2. Add tests to cover these lines
3. Refactor code to be more testable
4. Add integration tests for complex scenarios

## Test Organization

### PowerDown.Core.Tests

Tests core business logic:

- `Models/` - Test configuration and models
- `Services/` - Test orchestrator and logging
- `Helpers/` - Test helpers and utilities

### PowerDown.Platform.Windows.Tests

Tests Windows platform implementations:

- `Services/` - Test path detection and shutdown
- `Detectors/` - Test Steam detectors
- `Helpers/` - Test file system and registry mocks

### PowerDown.Platform.Linux.Tests

Tests Linux platform implementations:

- `Services/` - Test path detection and shutdown
- `Detectors/` - Test Steam detectors

### PowerDown.Platform.macOS.Tests

Tests macOS platform implementations:

- `Services/` - Test path detection and shutdown
- `Detectors/` - Test Steam detectors

### PowerDown.UI.Tests

Tests UI state and view models:

- `MainViewModelTests` - Monitoring flow and status updates
- `DownloadItemViewModelTests` - Status mapping and display

## Best Practices

### 1. Test Names Should Be Descriptive

Use format: `MethodName_Scenario_ExpectedResult()`

```
✅ DownloadOrchestrator_WithDryRun_DoesNotCallShutdown()
❌ TestMethod1()
```

### 2. Arrange-Act-Assert

Clearly separate test phases:

```
// Arrange
var sut = new SystemUnderTest();

// Act
var result = sut.DoSomething();

// Assert
result.Should().Be(expected);
```

### 3. Use FluentAssertions

Write readable assertions:

```
✅ result.Should().Be(expected);
✅ result.Should().NotBeNull();
✅ result.Should().Contain(expected);
❌ Assert.Equal(expected, result);
❌ Assert.NotNull(result);
```

### 4. Mock External Dependencies

Don't test file system, registry, network, etc.:

```csharp
// ✅ Good - Mock file system
mockFileSystem.Setup(f => f.FileExists(path)).Returns(true);

// ❌ Bad - Real file system in unit test
if (File.Exists(path))
{
    // ...
}
```

### 5. Test Edge Cases

Don't just test happy path:

```csharp
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public void Validate_WithInvalidInput_ReturnsFalse(string invalidInput)
{
    var result = Validator.Validate(invalidInput);
    result.Should().BeFalse();
}
```

### 6. Keep Tests Independent

Each test should be able to run independently:

```csharp
[Fact]
public void Test1()
{
    // ✅ Good - Creates fresh instance
    var sut = new SystemUnderTest();
    // ...
}

[Fact]
public void Test2()
{
    // ✅ Good - Creates fresh instance
    var sut = new SystemUnderTest();
    // ...
}
```

## Debugging Tests

### Running in Debug Mode

```bash
dotnet test -c Debug
```

Attach debugger:
1. Set breakpoints in test code
2. Press F5 in Visual Studio
3. Or use "Test > Debug" in Test Explorer

### Verbose Test Output

Run tests with detailed output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Single Test Execution

Run only one test method:

```bash
dotnet test --filter "FullyQualifiedName~SpecificTestMethod"
```

## CI/CD Integration

### GitHub Actions

Tests run automatically on:

- Pull requests
- Push to main/develop branches
- Release tags

### Local CI Testing

To test CI locally:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Build solution
dotnet build -c Release
```

## Common Testing Issues

### File Access Denied

**Problem:** Tests fail with UnauthorizedAccessException

**Solution:** Don't access real system directories in tests. Use mock file system.

### Registry Access on Non-Windows

**Problem:** Tests fail when running on Linux/macOS

**Solution:** Mock registry access in tests. Windows-specific tests only run on Windows.

### Async Test Deadlocks

**Problem:** Tests hang when using `.Result` or `.Wait()`

**Solution:** Always use `await` in async tests. Never use `.Result`.

### Flaky Tests

**Problem:** Tests pass sometimes but fail other times

**Solutions:**
- Remove reliance on timing
- Use proper mocking instead of real file system
- Add proper setup and teardown

## Resources

### Documentation

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [.NET Testing Documentation](https://docs.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test)

### Tools

- **Coverlet** - Code coverage for .NET
- **ReportGenerator** - Generate coverage reports
- **GitHub Actions** - CI/CD platform
- **Visual Studio Test Explorer** - Built-in test runner

For questions about testing, please open an issue on GitHub.
