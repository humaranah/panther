using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio.Abstractions;

/// <summary>
/// Plays a single audio track: decode -> optional resample -> device. Deliberately has no concept
/// of a playlist/queue or "next"/"previous" - it only exposes <see cref="PlaybackCompleted"/> so a
/// future queue/library component can decide what to play next, rather than baking that logic in
/// here. All members are safe to call from a UI thread; state changes are surfaced via events so a
/// ViewModel can marshal them to the UI thread as needed.
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>The current playback state.</summary>
    PlaybackState State { get; }

    /// <summary>The path passed to the most recent successful <see cref="PlayAsync"/> call, or null if stopped.</summary>
    string? CurrentFilePath { get; }

    /// <summary>The current playback position within the track.</summary>
    TimeSpan Position { get; }

    /// <summary>The total duration of the current track, or null if nothing is loaded.</summary>
    TimeSpan? Duration { get; }

    /// <summary>Playback volume, clamped to [0.0, 1.0].</summary>
    double Volume { get; set; }

    /// <summary>
    /// Opens and plays the given file from the start, replacing whatever is currently playing.
    /// Failures (unsupported format, device unavailable, ...) surface as exceptions from this call
    /// - see <see cref="PlaybackError"/> for failures that happen later, mid-playback.
    /// </summary>
    Task PlayAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Pauses playback. No-op if not currently playing.</summary>
    void Pause();

    /// <summary>Resumes playback after a pause. No-op if not currently paused.</summary>
    void Resume();

    /// <summary>Stops playback and releases the current track. No-op if already stopped.</summary>
    void Stop();

    /// <summary>Seeks within the current track. Throws if nothing is loaded or the track isn't seekable.</summary>
    void Seek(TimeSpan position);

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    event EventHandler<PlaybackState>? StateChanged;

    /// <summary>Raised periodically while playing, for UI progress binding.</summary>
    event EventHandler<TimeSpan>? PositionChanged;

    /// <summary>
    /// Raised when the current track finishes on its own (reaches its end), as opposed to being
    /// interrupted by <see cref="Stop"/> or a new <see cref="PlayAsync"/> call. The player does not
    /// auto-advance to anything - a queue/library component should handle this and call
    /// <see cref="PlayAsync"/> for the next track if desired.
    /// </summary>
    event EventHandler? PlaybackCompleted;

    /// <summary>
    /// Raised when playback fails asynchronously after <see cref="PlayAsync"/> already returned
    /// successfully (e.g. a decode error mid-stream). Failures during <see cref="PlayAsync"/>
    /// itself surface as exceptions from that call instead.
    /// </summary>
    event EventHandler<Exception>? PlaybackError;

    /// <summary>
    /// Raised when the output device stops unexpectedly - most likely unplugged, or a driver
    /// error - while a track was playing. The player stops cleanly (no track auto-resumes onto a
    /// different device); a queue/library component or the user must call <see cref="PlayAsync"/>
    /// again once a device is available. Not all backends reliably detect this.
    /// </summary>
    event EventHandler? DeviceDisconnected;
}
