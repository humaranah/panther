using Microsoft.Extensions.Logging;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Exceptions;
using Panther.Infrastructure.BassWrapper;
using Panther.Infrastructure.BassWrapper.Models;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Panther.Infrastructure;

public class FileMusicPlayer : IMusicPlayer, IDisposable
{
    // Constants
    public const double VolumeChangeThreshold = 0.01;
    private const double SecondsChangeThreshold = 0.001;

    // Dependencies
    private readonly IBassProcessor _bass;
    private readonly ILogger<FileMusicPlayer> _logger;

    // State
    private IBassChannel? _channel;
    private PlaybackState _playbackState;
    private float _volume;
    private string? _trackSource;
    private double _durationInSeconds;
    private double _positionInSeconds;
    private bool _disposed;

    // Events
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? PlaybackEnded;

    public FileMusicPlayer(
        IBassProcessor bass,
        ILogger<FileMusicPlayer> logger)
    {
        _bass = bass;
        if (_bass is IBassNotifier notifier)
            notifier.OperationError += OnBassOperationError;
        _bass.Init(); // Ensure BASS is initialized
        _logger = logger;
    }

    #region Properties
    public float Volume
    {
        get => _volume;
        set
        {
            var volume = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_volume - volume) < VolumeChangeThreshold)
                return;
            _volume = volume;
            _channel?.SetVolume(volume);
            PropertyChanged?.Invoke(this, new(nameof(Volume)));
        }
    }

    public string? TrackSource
    {
        get => _trackSource;
        set
        {
            if (_trackSource != value)
            {
                _trackSource = value;
                PropertyChanged?.Invoke(this, new(nameof(TrackSource)));
                PropertyChanged?.Invoke(this, new(nameof(HasTrackLoaded)));
            }
        }
    }

    public double DurationInSeconds
    {
        get => _durationInSeconds;
        private set
        {
            if (Math.Abs(_durationInSeconds - value) > SecondsChangeThreshold)
            {
                _durationInSeconds = value;
                PropertyChanged?.Invoke(this, new(nameof(DurationInSeconds)));
            }
        }
    }

    public PlaybackState PlaybackState
    {
        get => _playbackState;
        private set
        {
            if (_playbackState != value)
            {
                _playbackState = value;
                PropertyChanged?.Invoke(this, new(nameof(PlaybackState)));
            }
        }
    }

    [MemberNotNullWhen(true, nameof(_channel))]
    public bool HasTrackLoaded =>
        !string.IsNullOrWhiteSpace(_trackSource) &&
        _channel is { Handle.IsEmpty: false };
    #endregion

    #region Dispose pattern
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel?.Dispose();
            }
            _disposed = true;
        }
    }
    #endregion

    #region Public methods
    public async Task<bool> LoadTrackAsync(string sourceString, CancellationToken token)
    {
        PlaybackState = PlaybackState.Stopped;
        try
        {
            return await Task.Run(() => LoadTrackInternal(sourceString, token), token);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Loading track {SourceString} was canceled", sourceString);
            return false;
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error loading track", sourceString, 0, ex);
        }
    }

    public void Play()
    {
        if (!HasTrackLoaded || PlaybackState == PlaybackState.Playing)
            return;
        try
        {
            _channel.Play();
            PlaybackState = PlaybackState.Playing;
            _logger.LogDebug("Playback started: {TrackSource}", _trackSource);
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error playing track", _trackSource, _positionInSeconds, ex);
        }
    }

    public void Pause()
    {
        if (!HasTrackLoaded || PlaybackState != PlaybackState.Playing)
            return;
        try
        {
            _channel.Pause();
            PlaybackState = PlaybackState.Paused;
            _logger.LogDebug("Playback paused");
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error pausing track", _trackSource, _positionInSeconds, ex);
        }
    }

    public void Stop()
    {
        if (!HasTrackLoaded || PlaybackState == PlaybackState.Stopped)
            return;
        try
        {
            _channel.Stop();
            PlaybackState = PlaybackState.Stopped;
            _logger.LogDebug("Playback stopped");
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error stopping track", _trackSource, _positionInSeconds, ex);
        }
    }

    public double GetDurationInSeconds()
    {
        return HasTrackLoaded
            ? _channel.GetChannelLengthInSeconds()
            : 0;
    }

    public double GetPositionInSeconds()
    {
        var position = HasTrackLoaded
            ? _channel.GetPositionInSeconds()
            : 0;

        _positionInSeconds = position;
        return _positionInSeconds;
    }

    public void SeekTo(double seconds)
    {
        if (!HasTrackLoaded) return;
        try
        {
            _channel.SetPositionInSeconds(seconds);
        }
        catch (Exception ex)
        {
            var pos = TimeSpan.FromSeconds(seconds);
            throw new MusicPlayerException(
                $"Error seeking playback to position: {pos}", _trackSource, _positionInSeconds, ex);
        }
    }
    #endregion

    #region Event handlers
    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _logger.LogDebug("Playback ended for track: {TrackSource}", _trackSource);
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private void OnBassOperationError(object? sender, BassOperationError e)
    {
        _logger.LogError("Channel error: {ErrorString}", e.ToString());
    }
    #endregion

    private void SubscribeChannelEvents(IBassChannel channel)
    {
        channel.PlaybackEnded += OnPlaybackEnded;
        if (channel is IBassNotifier notifier)
        {
            notifier.OperationError += OnBassOperationError;
        }
    }

    private void UnsubscribeChannelEvents(IBassChannel channel)
    {
        channel.PlaybackEnded -= OnPlaybackEnded;
        if (channel is IBassNotifier notifier)
        {
            notifier.OperationError -= OnBassOperationError;
        }
    }

    private bool LoadTrackInternal(string sourceString, CancellationToken token)
    {
        var volume = _volume;
        token.ThrowIfCancellationRequested();
        if (_channel != null)
        {
            volume = _channel.GetVolume();
            UnsubscribeChannelEvents(_channel);
            _channel.Free();
            _durationInSeconds = 0d;
            _positionInSeconds = 0d;
            _trackSource = null;
        }

        _channel = _bass.StreamCreateFile(sourceString);
        if (_channel is null)
        {
            _trackSource = null;
            return false;
        }
        Volume = volume;
        TrackSource = sourceString;
        DurationInSeconds = GetDurationInSeconds();
        SubscribeChannelEvents(_channel);
        _logger.LogDebug("Track {SourceString} loaded successfully", _trackSource);
        return true;
    }
}
