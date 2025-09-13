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
    /// <summary>
    /// Gets or sets the volume level of the music player, ranging from 0.0 (mute) to 1.0 (maximum volume).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Gets the current playback state of the music player.
    /// </summary>
    PlaybackState PlaybackState { get; }

    /// <summary>
    /// Gets the source of the currently loaded track, or null if no track is loaded.
    /// </summary>
    string? TrackSource { get; }

    /// <summary>
    /// Gets the duration of the currently loaded track in seconds. Returns 0 if no track is loaded.
    /// </summary>
    double DurationInSeconds { get; }

    /// <summary>
    /// Indicates whether a track is currently loaded in the music player.
    /// </summary>
    bool HasTrackLoaded { get; }

    /// <summary>
    /// Event triggered when the playback of the current track ends.
    /// </summary>

    event EventHandler? PlaybackEnded;

    /// <summary>
    /// Asynchronously loads a track from the specified source string.
    /// </summary>
    /// <param name="sourceString">
    /// The source string representing the track to be loaded. This could be a file path, URL, or other identifier
    /// </param>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous load operation. The task result is true if the track was loaded
    /// </returns>
    Task<bool> LoadTrackAsync(string sourceString, CancellationToken token);

    /// <summary>
    /// Begins playback of the loaded track. If the track is already playing, this method has no effect.
    /// </summary>
    void Play();

    /// <summary>
    /// Pauses playback of the current track. If the track is already paused or stopped, this method has no effect.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops playback of the current track and resets the playback position to the beginning.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current playback position in seconds.
    /// </summary>
    /// <returns>
    /// The current playback position in seconds. If no track is loaded, returns 0.
    /// </returns>
    double GetPositionInSeconds();

    /// <summary>
    /// Seeks to the specified position in the currently loaded track.
    /// </summary>
    /// <param name="seconds">
    /// The position in seconds to seek to. If the specified position is out of bounds
    /// (less than 0 or greater than the track duration), it will be clamped to the valid range.
    /// </param>
    void SeekTo(double seconds);
}
