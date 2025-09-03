namespace Panther.Infrastructure.Tests;

public class PlaybackTimerTests
{
    [Fact]
    public async Task Start_ShouldStartTimer()
    {
        // Arrange
        using var playbackTimer = new PlaybackTimer();
        var elapsedEventTriggered = false;
        playbackTimer.Elapsed += (s, e) => elapsedEventTriggered = true;
        // Act
        playbackTimer.Start();
        // Assert
        await Task.Delay(250);
        Assert.True(elapsedEventTriggered);
    }

    [Fact]
    public async Task Stop_ShouldStopTimer()
    {
        // Arrange
        using var playbackTimer = new PlaybackTimer();
        var elapsedEventTriggered = false;
        playbackTimer.Elapsed += (s, e) => elapsedEventTriggered = true;
        playbackTimer.Start();
        // Act
        playbackTimer.Stop();
        elapsedEventTriggered = false;
        // Assert
        await Task.Delay(250);
        Assert.False(elapsedEventTriggered);
    }
}
