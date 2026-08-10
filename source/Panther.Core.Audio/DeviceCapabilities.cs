namespace Panther.Core.Audio;

/// <summary>
/// Represents the capabilities of an audio device, including supported sample rates, bit depths, channel counts, and other features.
/// </summary>
/// <param name="SupportedSampleRates">The sample rates supported by the device.</param>
/// <param name="MinBitsPerSample">The minimum bit depth supported by the device.</param>
/// <param name="MaxBitsPerSample">The maximum bit depth supported by the device.</param>
/// <param name="MaxChannels">The maximum number of channels supported by the device.</param>
/// <param name="SupportsExclusiveMode">Indicates whether the device supports exclusive mode.</param>
public readonly record struct DeviceCapabilities(
    int[] SupportedSampleRates,
    int MinBitsPerSample,
    int MaxBitsPerSample,
    int MaxChannels,
    bool SupportsExclusiveMode
);