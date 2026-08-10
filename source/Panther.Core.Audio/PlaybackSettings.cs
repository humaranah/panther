using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio;

/// <summary>
/// Represents the settings for audio playback, including exclusive mode preference, resampling quality, and other playback options.
/// </summary>
public class PlaybackSettings
{
    /// <summary>
    /// Gets or sets the preference for exclusive mode during audio playback.
    /// This setting determines whether the application should prefer exclusive mode,
    /// shared mode, or allow the system to decide.
    /// </summary>
    public ExclusiveModePreference ExclusiveMode { get; set; } = ExclusiveModePreference.PreferExclusive;

    /// <summary>
    /// Gets or sets the quality of resampling to be used during audio playback.
    /// </summary>
    public ResampleQuality ResampleQuality { get; set; } = ResampleQuality.Sinc32;

    /// <summary>
    /// Gets or sets a value indicating whether resampling is allowed during audio playback.
    /// </summary>
    public bool AllowResampling { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred audio device ID for playback.
    /// </summary>
    public string? PreferredDeviceId { get; set; }
}
