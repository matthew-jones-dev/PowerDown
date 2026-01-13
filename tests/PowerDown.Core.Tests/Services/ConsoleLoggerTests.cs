using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Moq;
using PowerDown.Core;
using PowerDown.Core.Services;

namespace PowerDown.Core.Tests.Services;

public class ConsoleLoggerTests
{
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOutput;
    private readonly ConsoleLogger _logger;
    private readonly ConsoleLogger _verboseLogger;

    public ConsoleLoggerTests()
    {
        _stringWriter = new StringWriter();
        _originalOutput = Console.Out;
        Console.SetOut(_stringWriter);
        
        _logger = new ConsoleLogger(false);
        _verboseLogger = new ConsoleLogger(true);
    }

    [Fact]
    public void LogInfo_LogsCorrectFormat()
    {
        _logger.LogInfo("Test message");
        
        var output = _stringWriter.ToString();
        output.Should().Contain("[INFO]");
        output.Should().Contain("Test message");
    }

    [Fact]
    public void LogWarning_LogsCorrectFormat()
    {
        _logger.LogWarning("Warning message");
        
        var output = _stringWriter.ToString();
        output.Should().Contain("[WARN]");
        output.Should().Contain("Warning message");
    }

    [Fact]
    public void LogError_LogsCorrectFormat()
    {
        _logger.LogError("Error message");
        
        var output = _stringWriter.ToString();
        output.Should().Contain("[ERROR]");
        output.Should().Contain("Error message");
    }

    [Fact]
    public void LogVerbose_WhenVerboseFalse_DoesNotLog()
    {
        _logger.LogVerbose("Verbose message");
        
        var output = _stringWriter.ToString();
        output.Should().NotContain("Verbose message");
    }

    [Fact]
    public void LogVerbose_WhenVerboseTrue_LogsCorrectFormat()
    {
        _verboseLogger.LogVerbose("Verbose message");
        
        var output = _stringWriter.ToString();
        output.Should().Contain("[VERBOSE]");
        output.Should().Contain("Verbose message");
    }

    [Fact]
    public void LogSuccess_LogsCorrectFormat()
    {
        _logger.LogSuccess("Success message");
        
        var output = _stringWriter.ToString();
        output.Should().Contain("[SUCCESS]");
        output.Should().Contain("Success message");
    }
}
