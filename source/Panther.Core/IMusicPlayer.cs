using Panther.Core.Enums;
using Panther.Core.Models;

namespace Panther.Core;

public interface IMusicPlayer
{
    event EventHandler<double>? PositionChanged;
    event EventHandler<PlaybackState>? PlaybackStateChanged;
    event EventHandler<Track?>? TrackChanged;
    event EventHandler? PlaybackEnded;

    int Volume { get; set; }
    Track? TrackInfo { get; }
    double Position { get; }
    PlaybackState PlaybackState { get; }

    Task InitializeAsync(CancellationToken token);
    Task LoadTrackAsync(string sourceString, CancellationToken token);
    void Play();
    void Pause();
    void Stop();
    void Seek(double position);
}
