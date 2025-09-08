using Microsoft.Extensions.Logging;
using Moq;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Exceptions;
using Panther.Core.Models;
using Panther.Infrastructure.BassWrapper;
using Panther.Infrastructure.BassWrapper.Models;
using Shouldly;
using Un4seen.Bass;

namespace Panther.Infrastructure.Tests;

public sealed class FileMusicPlayerTests : IDisposable
{
    private readonly Mock<IBassProcessor> _bassMock = new();
    private readonly Mock<IPlaybackTimer> _timerMock = new();
    private readonly Mock<IBassChannel> _channelMock = new();
    private readonly Mock<IBassNotifier> _notifierMock;
    private readonly Mock<ILogger<FileMusicPlayer>> _loggerMock = new();
    private readonly FileMusicPlayer _musicPlayer;

    private const string FakeFilePath = "fake.mp3";

    public FileMusicPlayerTests()
    {
        _notifierMock = _channelMock.As<IBassNotifier>();
        _musicPlayer = new FileMusicPlayer(_bassMock.Object, _timerMock.Object, _loggerMock.Object);
        _bassMock
            .Setup(x => x.StreamCreateFile(It.IsAny<string>(), It.IsAny<BASSFlag>()))
            .Callback(() => _channelMock.SetupGet(x => x.Handle).Returns(1))
            .Returns(_channelMock.Object);
    }

    #region Volume Tests
    [Fact]
    public void GetVolume_ShouldGetFromBass()
    {
        // Arrange
        var actualVolume = 0f;
        _bassMock.Setup(x => x.GetVolume()).Returns(0.5f);
        // Act
        actualVolume = _musicPlayer.Volume;
        // Assert
        actualVolume.ShouldBe(0.5f);
    }

    [Fact]
    public void SetVolume_ShouldSetToBass()
    {
        // Arrange
        var actualVolume = 0f;
        _bassMock.Setup(x => x.SetVolume(It.IsAny<float>())).Callback<float>(x => actualVolume = x);
        // Act
        _musicPlayer.Volume = 0.5f;
        // Assert
        actualVolume.ShouldBe(0.5f);
    }
    #endregion

    #region Position Tests
    [Fact]
    public void Position_ShouldReturnZeroWhenNoChannel()
    {
        // Act
        var position = _musicPlayer.GetPositionInSeconds();
        // Assert
        position.ShouldBe(0);
    }

    [Fact]
    public async Task Position_ShouldReturnChannelPosition()
    {
        // Arrange
        _channelMock.Setup(x => x.GetPositionInSeconds()).Returns(42.0);
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        // Act
        var position = _musicPlayer.GetPositionInSeconds();
        // Assert
        position.ShouldBe(42.0);
    }
    #endregion

    #region PlaybackState Tests
    [Fact]
    public async Task PlaybackState_ShouldRaiseEventWhenChanged()
    {
        // Arrange
        var wasCalled = false;
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        _musicPlayer.Play();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        wasCalled.ShouldBeTrue();
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Playing);
    }
    #endregion

    #region LoadTrackAsync Tests
    [Fact]
    public async Task LoadTrackAsync_ShouldLoadTrack()
    {
        // Arrange
        var expected = new TrackChange(null, FakeFilePath);
        TrackChange? actual = null;
        _musicPlayer.TrackChanged += (sender, trackChange) => actual = trackChange;
        // Act
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        actual.ShouldSatisfyAllConditions(
            x => x.ShouldNotBeNull(),
            x => x.ShouldBeEquivalentTo(expected));
    }

    [Fact]
    public async Task LoadTrackAsync_ShouldSubscribeToChannelEvents()
    {
        // Arrange
        _notifierMock
            .SetupAdd(x => x.OperationError += It.IsAny<EventHandler<BassOperationError>>())
            .Verifiable();
        // Act
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        // Assert
        _notifierMock.Verify();
    }

    [Fact]
    public async Task LoadTrackAsync_ShouldUnsubscribeToChannelEventsBeforeLoad()
    {
        // Arrange
        _notifierMock
            .SetupRemove(x => x.OperationError -= It.IsAny<EventHandler<BassOperationError>>())
            .Verifiable();
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        // Act
        await _musicPlayer.LoadTrackAsync("OtherTrack.mp3", CancellationToken.None);
        // Assert
        _notifierMock.Verify();
    }

    [Fact]
    public async Task LoadTrackAsync_ShouldNotThrowOnTaskCancelledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        // Act
        Func<Task> act = () => _musicPlayer.LoadTrackAsync(FakeFilePath, cts.Token);
        // Assert
        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task LoadTrackAsync_ShouldThrowMusicPlayerExceptionOnException()
    {
        // Arrange
        _bassMock.Setup(x => x.StreamCreateFile(It.IsAny<string>(), It.IsAny<BASSFlag>()))
            .Throws(new Exception("Test exception"));
        // Act
        Func<Task> act = () => _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        // Assert
        var exception = await act.ShouldThrowAsync<MusicPlayerException>();
        exception.ShouldSatisfyAllConditions(
            e => e.Operation.ShouldBe(nameof(_musicPlayer.LoadTrackAsync)),
            e => e.InnerException.ShouldNotBeNull(),
            e => e.InnerException!.Message.ShouldBe("Test exception"));
    }
    #endregion

    #region Play Tests
    [Theory]
    [InlineData(PlaybackState.Stopped, PlaybackState.Playing)]
    [InlineData(PlaybackState.Paused, PlaybackState.Playing)]
    [InlineData(PlaybackState.Playing, PlaybackState.Playing)]
    public async Task Play_ShouldSetPlaybackStateToPlaying(PlaybackState previous, PlaybackState expected)
    {
        // Arrange
        var wasCalled = false;
        await InitializePlayerOnState(previous);
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        _musicPlayer.Play();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(expected);
        wasCalled.ShouldBe(previous != PlaybackState.Playing);
    }

    [Fact]
    public async Task Play_ShouldNotTryToPlayWhenNoTrackLoaded()
    {
        // Arrange
        var wasCalled = false;
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        _musicPlayer.Play();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        wasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Play_ShouldRaiseMusicPlayerExceptionWhenExceptionIsThrown()
    {
        // Arrange
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        _channelMock.Setup(x => x.Play()).Throws(new Exception("Test exception"));
        // Act
        Action act = () => _musicPlayer.Play();
        // Assert
        var exception = act.ShouldThrow<MusicPlayerException>();
        exception.ShouldSatisfyAllConditions(
            e => e.Operation.ShouldBe(nameof(_musicPlayer.Play)),
            e => e.InnerException.ShouldNotBeNull(),
            e => e.InnerException!.Message.ShouldBe("Test exception"));
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
    }
    #endregion

    #region Pause Tests
    [Theory]
    [InlineData(PlaybackState.Playing, PlaybackState.Paused)]
    [InlineData(PlaybackState.Paused, PlaybackState.Paused)]
    [InlineData(PlaybackState.Stopped, PlaybackState.Stopped)]
    public async Task Pause_ShouldUpdatePlaybackStateIfNotStopped(PlaybackState previous, PlaybackState expected)
    {
        // Arrange
        var wasCalled = false;
        await InitializePlayerOnState(previous);
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        _musicPlayer.Pause();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(expected);
        wasCalled.ShouldBe(previous == PlaybackState.Playing);
    }

    [Fact]
    public async Task Pause_ShouldNotTryToPauseWhenNoTrackLoaded()
    {
        // Arrange
        var wasCalled = false;
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        _musicPlayer.Pause();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        wasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Pause_ShouldRaiseMusicPlayerExceptionWhenExceptionIsThrown()
    {
        // Arrange
        await InitializePlayerOnState(PlaybackState.Playing);
        _channelMock.Setup(x => x.Pause()).Throws(new Exception("Test exception"));
        // Act
        Action act = () => _musicPlayer.Pause();
        // Assert
        var exception = act.ShouldThrow<MusicPlayerException>();
        exception.ShouldSatisfyAllConditions(
            e => e.Operation.ShouldBe(nameof(_musicPlayer.Pause)),
            e => e.InnerException.ShouldNotBeNull(),
            e => e.InnerException!.Message.ShouldBe("Test exception"));
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Playing);
    }
    #endregion

    #region Stop Tests
    [Theory]
    [InlineData(PlaybackState.Stopped)]
    [InlineData(PlaybackState.Paused)]
    [InlineData(PlaybackState.Playing)]
    public async Task Stop_ShouldSetPlaybackStateToStopped(PlaybackState initialState)
    {
        // Arrange
        var wasCalled = false;
        await InitializePlayerOnState(initialState);
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        _musicPlayer.Stop();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        wasCalled.ShouldBe(initialState != PlaybackState.Stopped);
    }

    [Fact]
    public async Task Stop_ShouldNotTryToStopWhenNoTrackLoaded()
    {
        // Arrange
        var wasCalled = false;
        _musicPlayer.PlaybackStateChanged += (sender, state) => wasCalled = true;
        // Act
        _musicPlayer.Stop();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        wasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Stop_ShouldRaiseMusicPlayerExceptionWhenExceptionIsThrown()
    {
        // Arrange
        await InitializePlayerOnState(PlaybackState.Playing);
        _channelMock.Setup(x => x.Stop()).Throws(new Exception("Test exception"));
        // Act
        Action act = () => _musicPlayer.Stop();
        // Assert
        var exception = act.ShouldThrow<MusicPlayerException>();
        exception.ShouldSatisfyAllConditions(
            x => x.Operation.ShouldBe(nameof(_musicPlayer.Stop)),
            x => x.InnerException.ShouldNotBeNull(),
            x => x.InnerException!.Message.ShouldBe("Test exception"));
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Playing);
    }
    #endregion

    #region Seek Tests
    [Fact]
    public async Task Seek_ShouldUpdatePosition()
    {
        // Arrange
        var actual = 0D;
        var expected = 10D;
        var wasCalled = false;
        _channelMock.Setup(x => x.SetPositionInSeconds(It.IsAny<double>()))
            .Callback<double>(x => actual = x);
        _musicPlayer.PositionChanged += (sender, pos) => wasCalled = true;
        // Action
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        _musicPlayer.Seek(expected);
        await Task.Delay(5);
        // Assert
        actual.ShouldBe(expected);
        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Seek_ShouldThrowMusicPlayerExceptionOnException()
    {
        // Arrange
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        _channelMock.Setup(x => x.SetPositionInSeconds(It.IsAny<double>()))
            .Throws(new Exception("Test exception"));
        // Act
        Action act = () => _musicPlayer.Seek(10D);
        // Assert
        var exception = act.ShouldThrow<MusicPlayerException>();
        exception.ShouldSatisfyAllConditions(
            e => e.Operation.ShouldBe(nameof(_musicPlayer.Seek)),
            e => e.InnerException.ShouldNotBeNull(),
            e => e.InnerException!.Message.ShouldBe("Test exception"));
    }
    #endregion

    #region PositionChanged Event Tests
    [Theory]
    [InlineData(0, 100, PlaybackState.Stopped, false)]
    [InlineData(50, 100, PlaybackState.Playing, false)]
    [InlineData(50, 100, PlaybackState.Paused, false)]
    [InlineData(100, 100, PlaybackState.Stopped, false)]
    [InlineData(100, 100, PlaybackState.Playing, true)]
    [InlineData(150, 100, PlaybackState.Paused, true)]
    public async Task PositionChanged_ShouldBeRaisedByPlaybackTimer(
        double position, double duration, PlaybackState playbackState, bool shouldRaiseEnded)
    {
        // Arrange
        var expected = position >= duration ? 0d : position;
        var actual = 0d;
        var playbackEndedRaised = false;
        _channelMock.Setup(x => x.GetChannelLengthInSeconds()).Returns(duration);
        _channelMock.Setup(x => x.GetPositionInSeconds()).Returns(position);
        _musicPlayer.PositionChanged += (sender, pos) => actual = pos;
        _musicPlayer.PlaybackEnded += (sender, args) => playbackEndedRaised = true;
        await InitializePlayerOnState(playbackState);
        // Act
        _timerMock.Raise(x => x.Elapsed += null, EventArgs.Empty);
        // Assert
        actual.ShouldBe(expected);
        playbackEndedRaised.ShouldBe(shouldRaiseEnded);
    }

    [Fact]
    public void PositionChanged_ShouldNotBeRaisedWhenTrackNotLoaded()
    {
        // Arrange
        var wasCalled = false;
        _musicPlayer.PositionChanged += (sender, pos) => wasCalled = true;
        // Act
        _timerMock.Raise(x => x.Elapsed += null, EventArgs.Empty);
        // Assert
        wasCalled.ShouldBeFalse();
    }
    #endregion

    private async Task InitializePlayerOnState(PlaybackState playbackState)
    {
        if (!_musicPlayer.HasTrackLoaded)
            await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        switch (playbackState)
        {
            case PlaybackState.Playing:
                _musicPlayer.Play();
                break;
            case PlaybackState.Paused:
                _musicPlayer.Play();
                _musicPlayer.Pause();
                break;
        }
    }

    public void Dispose()
    {
        _musicPlayer.Dispose();
        GC.SuppressFinalize(this);
    }
}
