using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using PowerDown.Abstractions;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Platform.Windows.Tests.Services;

public class WindowsShutdownServiceTests
{
    [Fact]
    public void IsShutdownScheduled_InitialState_IsFalse()
    {
        var service = new WindowsShutdownService();
        service.IsShutdownScheduled.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleShutdownAsync_WithValidDelay_SetsIsScheduledToTrue()
    {
        var service = new WindowsShutdownService();
        
        await service.ScheduleShutdownAsync(30, "Test message");
        
        service.IsShutdownScheduled.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ScheduleShutdownAsync_WithInvalidDelay_ThrowsArgumentException(int delay)
    {
        var service = new WindowsShutdownService();
        
        Func<Task> act = async () => await service.ScheduleShutdownAsync(delay, "Test");
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*delay must be greater than 0*");
    }

    [Theory]
    [InlineData(30, "Test message")]
    [InlineData(60, "Shutdown in 1 minute")]
    [InlineData(120, "")]
    public async Task ScheduleShutdownAsync_WithValidParameters_DoesNotThrow(int delay, string message)
    {
        var service = new WindowsShutdownService();
        
        Func<Task> act = async () => await service.ScheduleShutdownAsync(delay, message);
        
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancelShutdownAsync_ResetsIsScheduledToFalse()
    {
        var service = new WindowsShutdownService();
        
        await service.ScheduleShutdownAsync(30, "Test");
        service.IsShutdownScheduled.Should().BeTrue();
        
        await service.CancelShutdownAsync();
        service.IsShutdownScheduled.Should().BeFalse();
    }
}
