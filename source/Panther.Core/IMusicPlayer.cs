using Panther.Core.Enums;
using System.ComponentModel;

namespace Panther.Core;

/// <summary>
/// Defines the contract for a music player capable of loading, playing, pausing, stopping, and seeking tracks.
/// </summary>
/// <remarks>This interface provides methods and properties to control playback, manage track loading, and monitor
/// playback state. It also includes events for notifying when playback ends and implements <see
/// cref="INotifyPropertyChanged"/> to allow consumers to observe changes to its properties.</remarks>
public interface IMusicPlayer : INotifyPropertyChanged
{
    event EventHandler<double>? PositionChanged;
    event EventHandler? PlaybackEnded;

    float Volume { get; set; }
    PlaybackState PlaybackState { get; }
    string? TrackSource { get; }
    double DurationInSeconds { get; }
    double PositionInSeconds { get; }
    bool HasTrackLoaded { get; }


    Task<bool> LoadTrackAsync(string sourceString, CancellationToken token);
    void Play();
    void Pause();
    void Stop();
    void Seek(double seconds);
}
