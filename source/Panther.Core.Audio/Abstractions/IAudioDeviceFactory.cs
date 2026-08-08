namespace Panther.Core.Audio.Abstractions;

/// <summary>
/// Represents a factory for creating audio devices.
/// </summary>
public interface IAudioDeviceFactory
{
    /// <summary>
    /// Creates a new, unconfigured playback device.
    /// </summary>
    IAudioDevice CreateDevice();
}
