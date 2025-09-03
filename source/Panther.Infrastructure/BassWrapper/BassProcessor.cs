using Panther.Infrastructure.BassWrapper.Extensions;
using Panther.Infrastructure.BassWrapper.Models;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public class BassProcessor : IBassProcessor, IBassNotifier, IDisposable
{
    public event EventHandler<BassOperationError>? OperationError;

    private bool _disposed;

    public bool IsInitialized => Bass.BASS_IsStarted() > 0;

    public bool Init()
    {
        if (IsInitialized) return true;
        if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_STEREO, nint.Zero))
        {
            this.GetErrorAndRaise("Cannot initialize BASS.", OperationError);
            return false;
        }
        return true;
    }

    public bool Free()
    {
        if (!Bass.BASS_Free())
        {
            this.GetErrorAndRaise("Cannot free BASS.", OperationError);
            return false;
        }
        return true;
    }

    public float GetVolume()
    {
        var volume = Bass.BASS_GetVolume();
        if (volume < 0)
        {
            this.GetErrorAndRaise("Failed to get volume", OperationError);
            return 0f;
        }
        return volume;
    }

    public bool SetVolume(float volume)
    {
        if (!Bass.BASS_SetVolume(volume))
        {
            this.GetErrorAndRaise("Failed to set volume", OperationError);
            return false;
        }
        return true;
    }

    public IBassChannel StreamCreateFile(string file, BASSFlag flags = BASSFlag.BASS_DEFAULT)
    {
        BassHandle handle = Bass.BASS_StreamCreateFile(file, 0, 0, flags);
        if (handle.IsEmpty)
        {
            this.GetErrorAndRaise($"Failed to create stream from file: {file}", OperationError);
            return null!;
        }
        return new BassChannel(handle);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            Free();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
