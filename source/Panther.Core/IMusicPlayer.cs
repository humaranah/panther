using Panther.Core.Enums;
using Panther.Core.Models;

namespace Panther.Core;

public interface IMusicPlayer
{
    event EventHandler<double>? PositionChanged;
    event EventHandler<PlaybackState>? PlaybackStateChanged;
    event EventHandler<TrackChange>? TrackChanged;
    event EventHandler? PlaybackEnded;

    float Volume { get; set; }
    PlaybackState PlaybackState { get; }
    bool HasTrackLoaded { get; }


    Task<bool> LoadTrackAsync(string sourceString, CancellationToken token);
    void Play();
    void Pause();
    void Stop();
    double GetDurationInSeconds();
    double GetPositionInSeconds();
    void Seek(double position);
}
