using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Events;
using Un4seen.Bass;

namespace Panther.Core.Services;

public sealed class MusicPlayer : IMusicPlayer, IDisposable
{
    private int _stream;

    public MusicPlayer()
    {
        Bass.BASS_Init(-1, -1, BASSInit.BASS_DEVICE_STEREO | BASSInit.BASS_DEVICE_FREQ, IntPtr.Zero);
        FileName = string.Empty;
    }

    public string FileName { get; private set; }
    public PlayerStatus Status { get; private set; }
    public long TrackLength => Bass.BASS_ChannelGetLength(_stream);
    public TimeSpan TotalTime => TimeSpan.FromSeconds(Bass.BASS_ChannelBytes2Seconds(_stream, TrackLength));
    public long PlaybackPosition => Bass.BASS_ChannelGetPosition(_stream);
    public TimeSpan ElapsedTime => TimeSpan.FromSeconds(Bass.BASS_ChannelBytes2Seconds(_stream, PlaybackPosition));
    public float Volume
    {
        get => Bass.BASS_GetVolume();
        set => Bass.BASS_SetVolume(value);
    }

    public event EventHandler<PlayerStatusChangedEventArgs>? StatusChanged;

    public void Dispose()
    {
        if (_stream != 0)
        {
            Bass.BASS_StreamFree(_stream);
            _stream = 0;
        }
        Bass.BASS_Free();
    }

    public void Load(string fileName)
    {
        if (!File.Exists(fileName))
            throw new FileNotFoundException(ErrorMessages.CouldNotFind(fileName), fileName);
        _stream = Bass.BASS_StreamCreateFile(fileName, 0, 0, BASSFlag.BASS_MUSIC_PRESCAN);
        FileName = fileName;
    }

    public void Pause()
    {
        Bass.BASS_ChannelPause(_stream);
        ChangeStatus(PlayerStatus.Paused);
    }

    public void Play()
    {
        Bass.BASS_ChannelPlay(_stream, false);
        ChangeStatus(PlayerStatus.Playing);
    }

    public void Stop()
    {
        Bass.BASS_ChannelStop(_stream);
        ChangeStatus(PlayerStatus.Stopped);
    }

    private void ChangeStatus(PlayerStatus targetStatus)
    {
        var data = new PlayerStatusChangedEventArgs(Status, targetStatus);
        Status = targetStatus;
        StatusChanged?.Invoke(this, data);
    }
}
