using Microsoft.Extensions.Logging;
using Moq;
using Panther.Core;
using Shouldly;

namespace Panther.Infrastructure.Tests;

public sealed class PlaybackPositionTrackerTests : IDisposable
{
    private readonly Mock<IMusicPlayer> _musicPlayerMock = new();
    private readonly Mock<ILogger<PlaybackPositionTracker>> _loggerMock = new();
    private readonly PlaybackPositionTracker _playbackTracker;

    public PlaybackPositionTrackerTests()
    {
        _playbackTracker = new(_musicPlayerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Start_ShouldStartTimer()
    {
        // Act
        _playbackTracker.Start();
        // Assert
        _playbackTracker.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Stop_ShouldStopTimer()
    {
        // Arrange
        _playbackTracker.Start();
        // Act
        _playbackTracker.Stop();
        // Assert
        _playbackTracker.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task PositionUpdated_ShouldBeInvoked_WhenTimerElapses()
    {
        // Arrange
        var positionUpdatedCalled = false;
        _musicPlayerMock.Setup(x => x.GetPositionInSeconds()).Verifiable();
        _playbackTracker.PositionUpdated += (position) => positionUpdatedCalled = true;
        _playbackTracker.Start();
        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(_playbackTracker.Interval * 1.5));
        _playbackTracker.Stop();
        // Assert
        positionUpdatedCalled.ShouldBeTrue();
        _musicPlayerMock.Verify(x => x.GetPositionInSeconds(), Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _playbackTracker.Dispose();
    }
}
