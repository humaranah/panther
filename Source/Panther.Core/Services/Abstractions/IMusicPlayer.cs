using Panther.Core.Enums;
using Panther.Core.Events;

namespace Panther.Core.Services;

public interface IMusicPlayer
{
    string FileName { get; }
    PlayerStatus Status { get; }
    long TrackLength { get; }
    TimeSpan TotalTime { get; }
    long PlaybackPosition { get; }
    TimeSpan ElapsedTime { get; }
    float Volume { get; set; }

    event EventHandler<PlayerStatusChangedEventArgs> StatusChanged;

    void Load(string fileName);
    void Play();
    void Stop();
    void Pause();
}
