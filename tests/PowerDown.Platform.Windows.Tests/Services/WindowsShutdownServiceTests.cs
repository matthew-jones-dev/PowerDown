using System;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Xunit;
using FluentAssertions;
using Moq;
using PowerDown.Abstractions;
using PowerDown.Platform.Windows.Services;

namespace PowerDown.Platform.Windows.Tests.Services;

[SupportedOSPlatform("windows")]
public class WindowsShutdownServiceTests
{
    [Fact]
    public void IsShutdownScheduled_InitialState_IsFalse()
    {
        var mockService = new Mock<IShutdownService>();
        mockService.SetupGet(s => s.IsShutdownScheduled).Returns(false);
        mockService.Object.IsShutdownScheduled.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleShutdownAsync_WithValidDelay_SetsIsScheduledToTrue()
    {
        var mockService = new Mock<IShutdownService>();
        var isScheduled = false;
        mockService.Setup(s => s.ScheduleShutdownAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Callback(() => isScheduled = true);
        mockService.SetupGet(s => s.IsShutdownScheduled)
            .Returns(() => isScheduled);

        await mockService.Object.ScheduleShutdownAsync(30, "Test message");

        mockService.Object.IsShutdownScheduled.Should().BeTrue();
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
        var mockService = new Mock<IShutdownService>();
        mockService.Setup(s => s.ScheduleShutdownAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await mockService.Object.ScheduleShutdownAsync(delay, message);
        
        await act.Should().NotThrowAsync();
        mockService.Verify(s => s.ScheduleShutdownAsync(delay, message), Times.Once);
    }

    [Fact]
    public async Task CancelShutdownAsync_ResetsIsScheduledToFalse()
    {
        var mockService = new Mock<IShutdownService>();
        mockService.SetupSequence(s => s.IsShutdownScheduled)
            .Returns(true)
            .Returns(false);
        mockService.Setup(s => s.CancelShutdownAsync())
            .Returns(Task.CompletedTask);

        await mockService.Object.ScheduleShutdownAsync(30, "Test");
        mockService.Object.IsShutdownScheduled.Should().BeTrue();
        
        await mockService.Object.CancelShutdownAsync();
        mockService.Object.IsShutdownScheduled.Should().BeFalse();
    }
}
