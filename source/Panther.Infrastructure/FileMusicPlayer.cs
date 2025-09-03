using Microsoft.Extensions.Logging;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Exceptions;
using Panther.Core.Models;
using Panther.Infrastructure.BassWrapper;
using Panther.Infrastructure.BassWrapper.Models;
using System.Diagnostics.CodeAnalysis;

namespace Panther.Infrastructure;

public class FileMusicPlayer : IMusicPlayer, IDisposable
{
    // Constants
    private const double PositionThreshold = 0.001;

    // Dependencies
    private readonly IBassProcessor _bass;
    private readonly IPlaybackTimer _playbackTimer;
    private readonly ILogger<FileMusicPlayer> _logger;

    // State
    private IBassChannel? _channel;
    private PlaybackState _playbackState;
    private string? _trackSource;
    private double _trackDuration;
    private double _lastKnownPosition;
    private bool _disposed;

    // Events
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<PlaybackState>? PlaybackStateChanged;
    public event EventHandler<TrackChange>? TrackChanged;
    public event EventHandler? PlaybackEnded;

    public FileMusicPlayer(
        IBassProcessor bass,
        IPlaybackTimer playbackTimer,
        ILogger<FileMusicPlayer> logger)
    {
        _bass = bass;
        if (_bass is IBassNotifier notifier)
            notifier.OperationError += OnBassOperationError;
        _bass.Init();
        _playbackTimer = playbackTimer;
        _playbackTimer.Elapsed += OnPlaybackTimerElapsed;
        _logger = logger;
    }

    public float Volume
    {
        get => _bass.GetVolume();
        set => _bass.SetVolume(Math.Clamp(value, 0f, 1f));
    }

    public PlaybackState PlaybackState
    {
        get => _playbackState;
        private set
        {
            if (_playbackState == value) return;
            _playbackState = value;
            PlaybackStateChanged?.Invoke(this, value);
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
        if (channel is IBassNotifier notifier)
        {
            notifier.OperationError += OnBassOperationError;
        }
    }

    private void UnsubscribeChannelEvents(IBassChannel channel)
    {
        if (channel is IBassNotifier notifier)
        {
            notifier.OperationError -= OnBassOperationError;
        }
    }

    private bool LoadTrackInternal(string sourceString, CancellationToken token)
    {
        var previousSource = _trackSource;
        token.ThrowIfCancellationRequested();
        if (_channel != null)
        {
            UnsubscribeChannelEvents(_channel);
            _channel.Free();
            _trackDuration = 0d;
            _lastKnownPosition = 0d;
            _trackSource = null;
        }

        _trackSource = sourceString;
        _channel = _bass.StreamCreateFile(_trackSource);
        if (_channel is null)
        {
            _trackSource = null;
            return false;
        }

        _trackDuration = GetDurationInSeconds();
        SubscribeChannelEvents(_channel);
        TrackChanged?.Invoke(this, new TrackChange(previousSource, _trackSource));
        _logger.LogDebug("Track {SourceString} loaded successfully", _trackSource);
        return true;
    }

    public void Play()
    {
        if (!HasTrackLoaded || PlaybackState == PlaybackState.Playing) return;
        try
        {
            _channel.Play();
            _playbackTimer.Start();
            PlaybackState = PlaybackState.Playing;
            _logger.LogDebug("Playback started: {TrackSource}", _trackSource);
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error playing track", _trackSource, _lastKnownPosition, ex);
        }
    }

    public void Pause()
    {
        if (!HasTrackLoaded || PlaybackState != PlaybackState.Playing) return;
        try
        {
            _channel.Pause();
            _playbackTimer.Stop();
            PlaybackState = PlaybackState.Paused;
            _logger.LogDebug("Playback paused");
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error pausing track", _trackSource, _lastKnownPosition, ex);
        }
    }

    public void Stop()
    {
        if (!HasTrackLoaded || PlaybackState == PlaybackState.Stopped) return;
        try
        {
            _channel.Stop();
            _playbackTimer.Stop();
            PlaybackState = PlaybackState.Stopped;
            _lastKnownPosition = 0;
            PositionChanged?.Invoke(this, _lastKnownPosition);
            _logger.LogDebug("Playback stopped");
        }
        catch (Exception ex)
        {
            throw new MusicPlayerException("Error stopping track", _trackSource, _lastKnownPosition, ex);
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

        _lastKnownPosition = position;
        return _lastKnownPosition;
    }

    public void Seek(double position)
    {
        if (!HasTrackLoaded) return;
        try
        {
            _playbackTimer.Stop();
            _channel.SetPositionInSeconds(position);
            _lastKnownPosition = position;
            PositionChanged?.Invoke(this, position);
            _playbackTimer.Start();
        }
        catch (Exception ex)
        {
            var pos = TimeSpan.FromSeconds(position);
            throw new MusicPlayerException(
                $"Error seeking playback to position: {pos}", _trackSource, _lastKnownPosition, ex);
        }
    }

    private void OnPlaybackTimerElapsed(object? sender, EventArgs e)
    {
        if (!HasTrackLoaded)
        {
            if (Math.Abs(_lastKnownPosition) > PositionThreshold)
            {
                _lastKnownPosition = 0d;
                PositionChanged?.Invoke(this, _lastKnownPosition);
            }
            _playbackTimer.Stop();
            return;
        }
        var previous = _lastKnownPosition;
        var current = _channel!.GetPositionInSeconds();
        _lastKnownPosition = current;
        if (_channel is { Handle.IsEmpty: false } && _lastKnownPosition < _trackDuration)
        {
            if (Math.Abs(current - previous) < PositionThreshold)
            {
                return;
            }
            PositionChanged?.Invoke(this, current);
            return;
        }
        if (_lastKnownPosition >= _trackDuration && PlaybackState != PlaybackState.Stopped)
        {
            Stop();
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnBassOperationError(object? sender, BassOperationError e)
    {
        _logger.LogError("Channel error: {ErrorString}", e.ToString());
    }
}
