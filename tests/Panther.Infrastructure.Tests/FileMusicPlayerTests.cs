using Microsoft.Extensions.Logging;
using Moq;
using Panther.Core.Enums;
using Panther.Core.Exceptions;
using Panther.Infrastructure.BassWrapper;
using Panther.Infrastructure.BassWrapper.Models;
using Shouldly;
using System.ComponentModel;
using Un4seen.Bass;

namespace Panther.Infrastructure.Tests;

public sealed class FileMusicPlayerTests : IDisposable
{
    private readonly Mock<IBassProcessor> _bassMock = new();
    private readonly Mock<IBassChannel> _channelMock = new();
    private readonly Mock<IBassNotifier> _notifierMock;
    private readonly Mock<ILogger<FileMusicPlayer>> _loggerMock = new();
    private readonly FileMusicPlayer _musicPlayer;

    private readonly HashSet<string> _propertiesChanged = [];

    private const string FakeFilePath = "fake.mp3";

    public FileMusicPlayerTests()
    {
        _notifierMock = _channelMock.As<IBassNotifier>();
        _musicPlayer = new FileMusicPlayer(_bassMock.Object, _loggerMock.Object);
        _bassMock
            .Setup(x => x.StreamCreateFile(It.IsAny<string>(), It.IsAny<BASSFlag>()))
            .Callback(() => _channelMock.SetupGet(x => x.Handle).Returns(1))
            .Returns(_channelMock.Object);
    }

    #region Volume Tests
    [Theory]
    [InlineData(0.0f, false)] // Initial volume is 0, setting to 0 should not trigger update
    [InlineData(0.5f, true)] // Change exceeds threshold, should update
    [InlineData(1.0f, true)] // Change exceeds threshold, should update
    [InlineData(-0.5f, false)] // Change is clamped to 0, which is the same as initial, should not update
    [InlineData(1.5f, true)] // Change is clamped to 1, exceeds threshold from initial 0, should update
    public async Task SetVolume_ShouldUpdate_WhenThresholdExceeded(float volumeToSet, bool shouldUpdate)
    {
        // Arrange
        const float initialVolume = 0f;
        var expectedVolume = Math.Clamp(volumeToSet, 0f, 1f);
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        _musicPlayer.Volume = initialVolume; // Set an initial volume
        _channelMock.Setup(x => x.SetVolume(It.IsAny<float>())).Verifiable();
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Volume = volumeToSet;
        // Assert
        _musicPlayer.Volume.ShouldBe(expectedVolume);
        if (shouldUpdate)
        {
            _propertiesChanged.ShouldContain(nameof(_musicPlayer.Volume));
            _channelMock.Verify(x => x.SetVolume(expectedVolume), Times.Once);
        }
        else
        {
            _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.Volume));
            _channelMock.Verify(x => x.SetVolume(expectedVolume), Times.Never);
        }
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
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Play();
        await Task.Delay(1); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Playing);
        _propertiesChanged.ShouldContain(nameof(_musicPlayer.PlaybackState));
    }
    #endregion

    #region LoadTrackAsync Tests
    [Fact]
    public async Task LoadTrackAsync_ShouldLoadTrack()
    {
        // Arrange
        AttachPropertyChangedListener();
        // Act
        var result = await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        await Task.Delay(1); // Allow some time for the event to be raised
        // Assert
        result.ShouldBeTrue();
        _musicPlayer.HasTrackLoaded.ShouldBeTrue();
        _musicPlayer.TrackSource.ShouldBe(FakeFilePath);
        _propertiesChanged.ShouldContain(nameof(_musicPlayer.TrackSource));
        _propertiesChanged.ShouldContain(nameof(_musicPlayer.HasTrackLoaded));
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
        await InitializePlayerOnState(previous);
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Play();
        await Task.Delay(1); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(expected);
        if (previous != PlaybackState.Playing)
            _propertiesChanged.ShouldContain(nameof(_musicPlayer.PlaybackState));
        else
            _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.PlaybackState));
    }

    [Fact]
    public async Task Play_ShouldNotTryToPlayWhenNoTrackLoaded()
    {
        // Arrange
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Play();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.PlaybackState));
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
    public async Task Pause_ShouldUpdatePlaybackStateIfNotStopped(PlaybackState current, PlaybackState expected)
    {
        // Arrange
        await InitializePlayerOnState(current);
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Pause();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(expected);
        if (current != expected)
            _propertiesChanged.ShouldContain(nameof(_musicPlayer.PlaybackState));
        else
            _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.PlaybackState));
    }

    [Fact]
    public async Task Pause_ShouldNotTryToPauseWhenNoTrackLoaded()
    {
        // Arrange
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Pause();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.PlaybackState));
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
        await InitializePlayerOnState(initialState);
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Stop();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        if (initialState != PlaybackState.Stopped)
            _propertiesChanged.ShouldContain(nameof(_musicPlayer.PlaybackState));
        else
            _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.PlaybackState));
    }

    [Fact]
    public async Task Stop_ShouldNotTryToStopWhenNoTrackLoaded()
    {
        // Arrange
        AttachPropertyChangedListener();
        // Act
        _musicPlayer.Stop();
        await Task.Delay(5); // Allow some time for the event to be raised
        // Assert
        _musicPlayer.PlaybackState.ShouldBe(PlaybackState.Stopped);
        _propertiesChanged.ShouldNotContain(nameof(_musicPlayer.PlaybackState));
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
        _channelMock.Setup(x => x.SetPositionInSeconds(It.IsAny<double>())).Verifiable();
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        // Action
        _musicPlayer.SeekTo(10D);
        // Assert
        _channelMock.Verify(x => x.SetPositionInSeconds(10D), Times.Once);
    }

    [Fact]
    public async Task Seek_ShouldThrowMusicPlayerExceptionOnException()
    {
        // Arrange
        await _musicPlayer.LoadTrackAsync(FakeFilePath, CancellationToken.None);
        _channelMock.Setup(x => x.SetPositionInSeconds(It.IsAny<double>()))
            .Throws(new Exception("Test exception"));
        // Act
        Action act = () => _musicPlayer.SeekTo(10D);
        // Assert
        var exception = act.ShouldThrow<MusicPlayerException>();
        exception.ShouldSatisfyAllConditions(
            e => e.Operation.ShouldBe(nameof(_musicPlayer.SeekTo)),
            e => e.InnerException.ShouldNotBeNull(),
            e => e.InnerException!.Message.ShouldBe("Test exception"));
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

    private void AttachPropertyChangedListener()
    {
        _musicPlayer.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName is not null)
                _propertiesChanged.Add(args.PropertyName);
        };
    }
}
