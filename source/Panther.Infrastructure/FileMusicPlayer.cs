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
    private const double VolumeThreshold = 0.01;
    private const double PositionThreshold = 0.001;

    // Dependencies
    private readonly IBassProcessor _bass;
    private readonly IPlaybackTimer _playbackTimer;
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
    public event EventHandler<double>? PositionChanged;
    public event EventHandler? PlaybackEnded;

    public FileMusicPlayer(
        IBassProcessor bass,
        IPlaybackTimer playbackTimer,
        ILogger<FileMusicPlayer> logger)
    {
        _bass = bass;
        if (_bass is IBassNotifier notifier)
            notifier.OperationError += OnBassOperationError;
        _bass.Init(); // Ensure BASS is initialized
        _playbackTimer = playbackTimer;
        _playbackTimer.Elapsed += OnPlaybackTimerElapsed;
        _logger = logger;
    }

    public float Volume
    {
        get => _volume;
        set
        {
            if (!HasTrackLoaded) return;
            if (Math.Abs(_volume - value) > VolumeThreshold)
            {
                _volume = value;
                _channel.SetVolume(value);
                PropertyChanged?.Invoke(this, new(nameof(Volume)));
            }
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
            if (Math.Abs(_durationInSeconds - value) > PositionThreshold)
            {
                _durationInSeconds = value;
                PropertyChanged?.Invoke(this, new(nameof(DurationInSeconds)));
            }
        }
    }

    public double PositionInSeconds
    {
        get => _positionInSeconds;
        private set
        {
            if (Math.Abs(_positionInSeconds - value) > PositionThreshold)
            {
                _positionInSeconds = value;
                PositionChanged?.Invoke(this, _positionInSeconds);
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
        token.ThrowIfCancellationRequested();
        if (_channel != null)
        {
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

        TrackSource = sourceString;
        DurationInSeconds = GetDurationInSeconds();
        SubscribeChannelEvents(_channel);
        _logger.LogDebug("Track {SourceString} loaded successfully", _trackSource);
        return true;
    }

    public void Play()
    {
        if (!HasTrackLoaded || PlaybackState == PlaybackState.Playing)
            return;
        try
        {
            _channel.Play();
            _playbackTimer.Start();
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
            _playbackTimer.Stop();
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
            _playbackTimer.Stop();
            PlaybackState = PlaybackState.Stopped;
            PositionInSeconds = 0d;
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

    public void Seek(double seconds)
    {
        if (!HasTrackLoaded) return;
        try
        {
            _playbackTimer.Stop();
            _channel.SetPositionInSeconds(seconds);
            PositionInSeconds = seconds;
            _playbackTimer.Start();
        }
        catch (Exception ex)
        {
            var pos = TimeSpan.FromSeconds(seconds);
            throw new MusicPlayerException(
                $"Error seeking playback to position: {pos}", _trackSource, _positionInSeconds, ex);
        }
    }

    private void OnPlaybackTimerElapsed(object? sender, EventArgs e)
    {
        if (!HasTrackLoaded || _playbackState != PlaybackState.Playing)
        {
            _playbackTimer.Stop();
            return;
        }
        PositionInSeconds = _channel.GetPositionInSeconds();
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _playbackTimer.Stop();
        _logger.LogDebug("Playback ended for track: {TrackSource}", _trackSource);
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
        if (sender is IBassChannel channel)
        {
            UnsubscribeChannelEvents(channel);
        }
    }

    private void OnBassOperationError(object? sender, BassOperationError e)
    {
        _logger.LogError("Channel error: {ErrorString}", e.ToString());
    }
}
