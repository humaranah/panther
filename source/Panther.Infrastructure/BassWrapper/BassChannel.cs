using Panther.Infrastructure.BassWrapper.Extensions;
using Panther.Infrastructure.BassWrapper.Models;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public class BassChannel : IBassChannel, IBassNotifier
{
    private readonly SYNCPROC? _endSyncProc;
    private BassHandle _endSyncHandle;
    private BassHandle _handle;
    private bool _disposed;

    public event EventHandler? PlaybackEnded;
    public event EventHandler<BassOperationError>? OperationError;

    public BassChannel(BassHandle handle)
    {
        _handle = handle;
        _endSyncProc = (handle, channel, data, user) =>
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        };
        AttachEvents();
    }

    public BassHandle Handle => _handle;

    public bool Play()
    {
        if (_handle.IsEmpty) return false;
        if (!Bass.BASS_ChannelPlay(_handle, false))
        {
            this.GetErrorAndRaise("Failed to play channel", OperationError);
            return false;
        }
        return true;
    }
    public bool Pause()
    {
        if (_handle.IsEmpty) return false;
        if (!Bass.BASS_ChannelPause(_handle))
        {
            this.GetErrorAndRaise("Failed to pause channel", OperationError);
            return false;
        }
        return true;
    }

    public bool Stop()
    {
        if (_handle.IsEmpty) return false;
        if (!Bass.BASS_ChannelStop(_handle))
        {
            this.GetErrorAndRaise("Failed to stop channel", OperationError);
            return false;
        }
        return true;
    }

    public double GetChannelLengthInSeconds()
    {
        if (_handle.IsEmpty) return 0d;
        var lengthBytes = Bass.BASS_ChannelGetLength(_handle);
        if (lengthBytes < 0)
        {
            this.GetErrorAndRaise("Failed to get channel length", OperationError);
            return 0d;
        }
        var seconds = Bass.BASS_ChannelBytes2Seconds(_handle, lengthBytes);
        if (seconds < 0)
        {
            this.GetErrorAndRaise("Failed to convert channel length to seconds", OperationError);
            return 0d;
        }
        return seconds;
    }

    public double GetPositionInSeconds()
    {
        if (_handle.IsEmpty) return 0d;
        var positionBytes = Bass.BASS_ChannelGetPosition(_handle);
        if (positionBytes < 0)
        {
            this.GetErrorAndRaise("Failed to get channel position", OperationError);
            return 0d;
        }
        var seconds = Bass.BASS_ChannelBytes2Seconds(_handle, positionBytes);
        if (seconds < 0)
        {
            this.GetErrorAndRaise("Failed to convert channel position to seconds", OperationError);
            return 0d;
        }
        return seconds;
    }

    public bool SetPositionInSeconds(double seconds)
    {
        if (_handle.IsEmpty) return false;
        var positionBytes = Bass.BASS_ChannelSeconds2Bytes(_handle, seconds);
        if (positionBytes < 0)
        {
            this.GetErrorAndRaise("Failed to convert seconds to channel position", OperationError);
            return false;
        }
        if (!Bass.BASS_ChannelSetPosition(_handle, positionBytes))
        {
            this.GetErrorAndRaise("Failed to set channel position", OperationError);
            return false;
        }
        return true;
    }

    public bool Free()
    {
        if (_handle.IsEmpty || !DetachEvents()) return false;
        if (!Bass.BASS_StreamFree(_handle))
        {
            this.GetErrorAndRaise("Failed to free channel", OperationError);
            _handle = BassHandle.Empty;
            return false;
        }
        _handle = BassHandle.Empty;
        return true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            Free();
        }
        _handle = BassHandle.Empty;
        _disposed = true;
    }

    private void AttachEvents()
    {
        if (_handle.IsEmpty) return;
        _endSyncHandle = Bass.BASS_ChannelSetSync(_handle, BASSSync.BASS_SYNC_END, 0, _endSyncProc, nint.Zero);
        if (_endSyncHandle.IsEmpty)
        {
            this.GetErrorAndRaise("Failed to attach sync events", OperationError);
        }
    }

    private bool DetachEvents()
    {
        if (_handle.IsEmpty) return false;
        if (_endSyncHandle.IsEmpty) return true;
        if (!Bass.BASS_ChannelRemoveSync(_handle, _endSyncHandle))
        {
            this.GetErrorAndRaise("Failed to remove end sync", OperationError);
            return false;
        }
        _endSyncHandle = BassHandle.Empty;
        return true;
    }
}
