namespace Panther.Core;

/// <summary>
/// Tracks the playback position of a music player and raises events at regular intervals.
/// </summary>
public interface IPlaybackPositionTracker
{
    /// <summary>
    /// Occurs when the playback position is updated and provides the current position in seconds.
    /// </summary>
    /// <remarks>
    /// This event is triggered at regular intervals when the tracker is started.
    /// </remarks>
    event Action<double> PositionUpdated;

    /// <summary>
    /// Gets the interval, in milliseconds, at which the playback position is updated.
    /// </summary>
    double Interval { get; }

    /// <summary>
    /// Indicates whether the playback position tracker is currently active and updating the position.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Starts the playback position tracking.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the playback position tracking.
    /// </summary>
    void Stop();
}
