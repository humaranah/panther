using Microsoft.Extensions.Logging;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Models;
using Panther.Infrastructure.BassWrapper;
using Panther.Infrastructure.Constants;
using Un4seen.Bass;
using Timer = System.Timers.Timer;

namespace Panther.Infrastructure;

public sealed class LocalMusicPlayer : IMusicPlayer, IDisposable
{
    private readonly IBassProcessor _bass;
    private readonly ILogger<LocalMusicPlayer> _logger;

    private readonly Timer _positionTimer;
    private IBassChannel? _channel;
    private Track? _trackInfo;
    private PlaybackState _playbackState;

    public event EventHandler<double>? PositionChanged;
    public event EventHandler<PlaybackState>? PlaybackStateChanged;
    public event EventHandler<Track?>? TrackChanged;
    public event EventHandler? PlaybackEnded;

    public LocalMusicPlayer(
        IBassProcessor bass,
        IBassNetService bassNet,
        ILogger<LocalMusicPlayer> logger)
    {
        _bass = bass;
        _logger = logger;
        bassNet.Register(BassCredentials.Email, BassCredentials.Key);
        _positionTimer = InitializeTimer();
    }

    public int Volume
    {
        get => (int)(_bass.GetVolume() * 100);
        set => _bass.SetVolume(value / 100f);
    }

    public Track? TrackInfo
    {
        get => _trackInfo;
        private set
        {
            _trackInfo = value;
            TrackChanged?.Invoke(this, TrackInfo);
        }
    }

    public double Position => _channel?.GetPositionInSeconds() ?? 0;

    public PlaybackState PlaybackState
    {
        get => _playbackState;
        private set
        {
            if (value != _playbackState)
            {
                _playbackState = value;
                PlaybackStateChanged?.Invoke(this, value);
            }
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _bass.Free();
        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        try
        {
            await Task.Run(() => InitializeInternal(token), token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing BASS");
            throw;
        }
    }

    private void InitializeInternal(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _bass.Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, nint.Zero);
    }

    public async Task LoadTrackAsync(string sourceString, CancellationToken token)
    {
        PlaybackState = PlaybackState.Stopped;
        try
        {
            await Task.Run(() => LoadTrackInternal(sourceString, token), token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading track {SourceString}", sourceString);
            throw;
        }
    }

    private void LoadTrackInternal(string sourceString, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _channel?.Free();
        _channel = _bass.StreamCreateFile(sourceString);
        TrackInfo = new() { Source = sourceString }; // Temporary, replace with actual metadata extraction
        _logger.LogDebug("Track {SourceString} loaded successfully", sourceString);
    }

    public void Pause()
    {
        if (PlaybackState != PlaybackState.Playing) return;
        try
        {
            _channel?.Pause();
            _positionTimer.Stop();
            PlaybackState = PlaybackState.Paused;
            _logger.LogDebug("Playback paused");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred trying to pause the current playback");
            throw;
        }
    }

    public void Play()
    {
        if (PlaybackState == PlaybackState.Playing) return;
        try
        {
            _channel?.Play();
            _positionTimer.Start();
            PlaybackState = PlaybackState.Playing;
            _logger.LogDebug("Playback started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred trying to play the current track.");
            throw;
        }
    }

    public void Stop()
    {
        if (PlaybackState == PlaybackState.Stopped) return;
        try
        {
            _channel?.Stop();
            _positionTimer.Stop();
            PlaybackState = PlaybackState.Stopped;
            _logger.LogDebug("Playback stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred trying to stop the current track.");
            throw;
        }
    }

    public void Seek(double position)
    {
        if (_channel == null || _channel.Handle.IsEmpty)
        {
            _logger.LogWarning("Cannot seek, no track loaded");
            return;
        }
        try
        {
            _channel.SetPositionInSeconds(position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred trying to seek to position {Position}", position);
            throw;
        }
    }

    private Timer InitializeTimer()
    {
        var timer = new Timer(200) { AutoReset = true };
        timer.Elapsed += (sender, e) =>
        {
            PositionChanged?.Invoke(this, Position);
            if (Position == (TrackInfo?.Duration ?? 0) && PlaybackState == PlaybackState.Playing)
            {
                timer.Stop();
                Stop();
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            }
        };
        return timer;
    }
}
