using NAudio.Wave;
using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Events;

namespace Panther.Core.Services;

public sealed class MusicPlayer : IMusicPlayer, IDisposable, IAsyncDisposable
{
    private readonly WaveOutEvent _wavePlayer;

    private AudioFileReader? _audioFile;

    public MusicPlayer(WaveOutEvent wavePlayer)
    {
        _wavePlayer = wavePlayer;
    }

    public string FileName => _audioFile?.FileName ?? "No media";
    public PlayerStatus Status => (PlayerStatus)_wavePlayer.PlaybackState;
    public long TrackLength => _audioFile?.Length ?? 0;
    public long TrackPosition => _audioFile?.Position ?? 0;
    public float Volume
    {
        get => _wavePlayer.Volume;
        set => _wavePlayer.Volume = value;
    }

    public event EventHandler<PlayerStatusChangedEventArgs>? StatusChanged;

    public void Dispose() => _audioFile?.Dispose();

    public ValueTask DisposeAsync() => _audioFile?.DisposeAsync() ?? ValueTask.CompletedTask;

    public void Load(string fileName)
    {
        if (!File.Exists(fileName))
            throw new FileNotFoundException(ErrorMessages.CouldNotFind(fileName), fileName);
        _audioFile = new AudioFileReader(fileName);
        _wavePlayer.Init(_audioFile);
    }

    public void Pause()
    {
        _wavePlayer.Pause();
        ChangeStatus(PlayerStatus.Paused);
    }

    public void Play()
    {
        _wavePlayer.Play();
        ChangeStatus(PlayerStatus.Playing);
    }

    public void Stop()
    {
        _wavePlayer.Stop();
        ChangeStatus(PlayerStatus.Stopped);
    }

    private void ChangeStatus(PlayerStatus targetStatus)
    {
        var data = new PlayerStatusChangedEventArgs(Status, targetStatus);
        StatusChanged?.Invoke(this, data);
    }
}
