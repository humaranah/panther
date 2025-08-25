using Microsoft.Extensions.Logging;
using Panther.Core;
using Panther.Core.Enums;
using Panther.Core.Models;
using Panther.Infrastructure.Constants;
using Panther.Infrastructure.Models;
using Un4seen.Bass;
using Timer = System.Timers.Timer;

namespace Panther.Infrastructure;

public sealed class LocalMusicPlayer : IMusicPlayer, IDisposable
{
    private readonly Timer _positionTimer;
    private readonly ILogger<LocalMusicPlayer> _logger;
    private ChannelHandle _handle;
    private Track? _trackInfo;
    private PlaybackState _playbackState;

    public event EventHandler<double>? PositionChanged;
    public event EventHandler<PlaybackState>? PlaybackStateChanged;
    public event EventHandler<Track?>? TrackChanged;
    public event EventHandler? PlaybackEnded;

    public LocalMusicPlayer(ILogger<LocalMusicPlayer> logger)
    {
        _logger = logger;
        _logger.LogDebug("Registering {InternalName}...", BassNet.InternalName);
        BassNet.Registration(BassCredentials.Email, BassCredentials.Key);
        _logger.LogDebug("Bass.Net registered successfully");
        _positionTimer = InitializeTimer();
    }

    public int Volume
    {
        get => (int)(Bass.BASS_GetVolume() * 100);
        set => Bass.BASS_SetVolume(value / 100f);
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

    public double Position
    {
        get
        {
            var position = Bass.BASS_ChannelGetPosition(_handle);
            return Bass.BASS_ChannelBytes2Seconds(_handle, position);
        }
    }

    public PlaybackState PlaybackState
    {
        get => _playbackState;
        private set
        {
            _playbackState = value;
            PlaybackStateChanged?.Invoke(this, value);
        }
    }

    public void Dispose()
    {
        Bass.BASS_StreamFree(_handle);
        Bass.BASS_Free();
        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            if (Bass.BASS_IsStarted() != 0) return;
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, nint.Zero))
            {
                var errorCode = Bass.BASS_ErrorGetCode();
                _logger.LogError("BASS initialization error: {ErrorCode}", errorCode);
            }
            var device = Bass.BASS_GetDevice();
            _logger.LogDebug("BASS initialized successfully: {@BassInfo}", Bass.BASS_GetDeviceInfo(device));
        });
    }

    public async Task LoadTrackAsync(string sourceString)
    {
        if (PlaybackState != PlaybackState.Stopped)
            PlaybackState = PlaybackState.Stopped;
        if (!_handle.IsEmpty && !Bass.BASS_StreamFree(_handle))
        {
            _logger.LogError("Error unloading track: {ErrorCode}", Bass.BASS_ErrorGetCode());
            return;
        }
        _handle = await Task.Run(
            () => Bass.BASS_StreamCreateFile(sourceString, 0L, 0L, BASSFlag.BASS_DEFAULT));
        if (_handle.IsEmpty)
        {
            var errorCode = Bass.BASS_ErrorGetCode();
            _logger.LogError("Error loading track {SourceString}: {ErrorCode}", sourceString, errorCode);
            return;
        }
        TrackInfo = new() { Source = sourceString };
        _logger.LogDebug("Track {SourceString} loaded successfully", sourceString);
    }

    public void Pause()
    {
        if (_handle.IsEmpty)
        {
            _logger.LogWarning("Cannot pause, no track loaded");
            return;
        }
        if (!Bass.BASS_ChannelPause(_handle))
        {
            var errorCode = Bass.BASS_ErrorGetCode();
            _logger.LogError("Error pausing playback: {ErrorCode}", errorCode);
            return;
        }
        _positionTimer.Stop();
        PlaybackState = PlaybackState.Paused;
        _logger.LogDebug("Playback paused");
    }

    public void Play()
    {
        if (_handle.IsEmpty)
        {
            _logger.LogWarning("Cannot play, no track loaded");
            return;
        }
        if (!Bass.BASS_ChannelPlay(_handle, false))
        {
            var errorCode = Bass.BASS_ErrorGetCode();
            _logger.LogError("Error starting playback: {ErrorCode}", errorCode);
            return;
        }
        _positionTimer.Start();
        PlaybackState = PlaybackState.Playing;
        _logger.LogDebug("Playback started");
    }

    public void Stop()
    {
        if (_handle.IsEmpty)
        {
            _logger.LogWarning("Cannot stop, no track loaded");
            return;
        }
        if (!Bass.BASS_ChannelStop(_handle))
        {
            var errorCode = Bass.BASS_ErrorGetCode();
            _logger.LogError("Error stopping playback: {ErrorCode}", errorCode);
            return;
        }
        PlaybackState = PlaybackState.Stopped;
        _logger.LogDebug("Playback stopped");
    }

    public void Seek(double position)
    {
        if (_handle.IsEmpty)
        {
            _logger.LogWarning("Cannot seek, no track loaded");
            return;
        }
        var bytePosition = Bass.BASS_ChannelSeconds2Bytes(_handle, position);
        if (!Bass.BASS_ChannelSetPosition(_handle, bytePosition))
        {
            var errorCode = Bass.BASS_ErrorGetCode();
            _logger.LogError("Error seeking to position {Position}: {ErrorCode}", position, errorCode);
            return;
        }
        _logger.LogDebug("Seeked to position {Position}", position);
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
