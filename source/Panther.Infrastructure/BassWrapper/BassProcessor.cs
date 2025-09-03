using Microsoft.Extensions.Logging;
using Panther.Infrastructure.Exceptions;
using Panther.Infrastructure.Models;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public sealed class BassProcessor(ILogger<BassProcessor> logger) : IBassProcessor, IDisposable
{
    public void Init(int device, int freq, BASSInit flags, nint win)
    {
        var success = Bass.BASS_Init(device, freq, flags, win);
        if (!success)
        {
            switch (Bass.BASS_ErrorGetCode())
            {
                case BASSError.BASS_ERROR_ALREADY:
                    logger.LogWarning("BASS is already initialized.");
                    return;
                default:
                    throw new BassException(Bass.BASS_ErrorGetCode());
            }
        }
        var currentDevice = Bass.BASS_GetDevice();
        logger.LogDebug("BASS initialized successfully: {@BassInfo}", Bass.BASS_GetDeviceInfo(currentDevice));
    }

    public void Free()
    {
        if (!Bass.BASS_Free())
        {
            var errorCode = Bass.BASS_ErrorGetCode();
            switch (errorCode)
            {
                case BASSError.BASS_ERROR_INIT:
                    logger.LogWarning("BASS was not initialized; it does not need to be unloaded.");
                    break;
                default:
                    logger.LogError("Unable to unload BASS: {ErrorCode}", errorCode);
                    break;
            }
        }
    }

    public float GetVolume() => Bass.BASS_GetVolume();

    public void SetVolume(float volume) => Bass.BASS_SetVolume(volume);

    public IBassChannel StreamCreateFile(string file,
        long offset = 0, long length = 0, BASSFlag flags = BASSFlag.BASS_DEFAULT)
    {
        ChannelHandle handle = Bass.BASS_StreamCreateFile(file, offset, length, flags);
        if (handle.IsEmpty)
        {
            throw new BassException(Bass.BASS_ErrorGetCode());
        }
        return new BassChannel(handle);
    }

    public void Dispose() => Free();
}
