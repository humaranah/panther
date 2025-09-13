using Microsoft.Extensions.Logging;
using Panther.Core;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Panther.Infrastructure;

public sealed class PlaybackPositionTracker : IPlaybackPositionTracker, IDisposable
{
    private readonly IMusicPlayer _musicPlayer;
    private readonly ILogger<PlaybackPositionTracker> _logger;

    private readonly Timer _timer;
    private bool _disposed;

    private const double DefaultInterval = 200;

    public event Action<double>? PositionUpdated;

    public PlaybackPositionTracker(
        IMusicPlayer musicPlayer,
        ILogger<PlaybackPositionTracker> logger)
    {
        _musicPlayer = musicPlayer;
        _logger = logger;
        _timer = new(DefaultInterval);
        _timer.Elapsed += OnTimerElapsed;
    }

    public double Interval => DefaultInterval;

    public bool IsActive => _timer.Enabled;

    public void Dispose()
    {
        if (!_disposed && _timer != null)
        {
            _timer?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        ThrowIfDisposed();
        if (IsActive) return;
        _timer.Start();
        _logger.LogDebug("Playback tracker started.");
    }

    public void Stop()
    {
        ThrowIfDisposed();
        if (!IsActive) return;
        _timer.Stop();
        _logger.LogDebug("Playback tracker stopped.");
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var seconds = _musicPlayer.GetPositionInSeconds();
        PositionUpdated?.Invoke(seconds);
    }


    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PlaybackPositionTracker));
    }
}
