namespace Panther.Core.Audio.Abstractions;

/// <summary>
/// Represents an audio device that can be used for audio playback or recording.
/// </summary>
public interface IAudioDevice : IDisposable
{
    /// <summary>
    /// Gets the capabilities of the audio device.
    /// </summary>
    /// <returns>The capabilities of the audio device.</returns>
    DeviceCapabilities GetCapabilities();

    /// <summary>
    /// Configures the audio device with the specified format and exclusive mode setting.
    /// </summary>
    /// <param name="format">The audio format to use.</param>
    /// <param name="exclusiveMode">Whether to use exclusive mode.</param>
    void Configure(AudioFormat format, bool exclusiveMode);

    /// <summary>
    /// The format the backend actually negotiated after <see cref="Configure"/>, which can differ
    /// from the requested format depending on the backend and share mode. Null until configured.
    /// </summary>
    AudioFormat? NegotiatedFormat { get; }

    /// <summary>
    /// Starts the audio device.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the audio device.
    /// </summary>
    void Stop();

    /// <summary>
    /// Occurs when the audio device requests more data to be played.
    /// </summary>
    event Action<Memory<float>, int> RequestData;

    /// <summary>
    /// Raised when the device stops unexpectedly - e.g. the output device was unplugged or a
    /// driver/backend error occurred - as opposed to <see cref="Stop"/> being called deliberately.
    /// Not all backends reliably detect this (miniaudio's own docs note at least one they don't),
    /// so the absence of this event is not proof the device is still working.
    /// </summary>
    event EventHandler? Disconnected;
}
