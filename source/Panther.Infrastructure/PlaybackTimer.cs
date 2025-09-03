using Panther.Core;
using Timer = System.Timers.Timer;

namespace Panther.Infrastructure;

public class PlaybackTimer : IPlaybackTimer, IDisposable
{
    private readonly Timer _timer;
    private bool _disposed;

    public event EventHandler? Elapsed;

    public PlaybackTimer()
    {
        _timer = new(200);
        _timer.Elapsed += (sender, args) => Elapsed?.Invoke(this, EventArgs.Empty);
    }

    public void Start()
    {
        ThrowIfDisposed();
        _timer.Start();
    }

    public void Stop()
    {
        ThrowIfDisposed();
        _timer.Stop();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PlaybackTimer));
    }
}
