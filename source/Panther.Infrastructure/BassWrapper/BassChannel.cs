using Panther.Infrastructure.Exceptions;
using Panther.Infrastructure.Models;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public sealed class BassChannel(ChannelHandle handle) : IBassChannel
{
    public ChannelHandle Handle => handle;

    public void Play()
    {
        if (handle.IsEmpty) return;
        var success = Bass.BASS_ChannelPlay(handle, false);
        if (!success)
        {
            throw new BassException(Bass.BASS_ErrorGetCode());
        }
    }
    public void Pause()
    {
        if (handle.IsEmpty) return;
        var success = Bass.BASS_ChannelPause(handle);
        if (!success)
        {
            throw new BassException(Bass.BASS_ErrorGetCode());
        }
    }

    public void Stop()
    {
        if (handle.IsEmpty) return;
        var success = Bass.BASS_ChannelStop(handle);
        if (!success)
        {
            throw new BassException(Bass.BASS_ErrorGetCode());
        }
    }

    public double GetPositionInSeconds()
    {
        if (handle.IsEmpty) return 0;
        var bytePosition = Bass.BASS_ChannelGetPosition(handle);
        if (bytePosition == -1)
            throw new BassException(Bass.BASS_ErrorGetCode());
        if (bytePosition == 0) return 0;
        var seconds = Bass.BASS_ChannelBytes2Seconds(handle, bytePosition);
        if (seconds < 0)
            throw new BassException(Bass.BASS_ErrorGetCode());
        return seconds;
    }

    public void SetPositionInSeconds(double seconds)
    {
        if (handle.IsEmpty) return;
        var bytePosition = Bass.BASS_ChannelSeconds2Bytes(handle, seconds);
        if (bytePosition == -1)
            throw new BassException(Bass.BASS_ErrorGetCode());
        var success = Bass.BASS_ChannelSetPosition(handle, bytePosition);
        if (!success)
            throw new BassException(Bass.BASS_ErrorGetCode());
    }

    public void Free()
    {
        if (handle.IsEmpty) return;
        var success = Bass.BASS_StreamFree(handle);
        if (!success)
        {
            throw new BassException(Bass.BASS_ErrorGetCode());
        }
    }

    public void Dispose() => Free();
}
