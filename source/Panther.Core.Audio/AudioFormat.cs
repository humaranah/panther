using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio;

/// <summary>
/// Represents the audio format, including sample rate, number of channels, sample format, bits per sample, and DSD transport mode.
/// </summary>
/// <param name="SampleRate">The sample rate of the audio format.</param>
/// <param name="Channels">The number of channels in the audio format.</param>
/// <param name="Format">The sample format of the audio format.</param>
/// <param name="BitsPerSample">The number of bits per sample in the audio format.</param>
public readonly record struct AudioFormat(
    int SampleRate,
    int Channels,
    SampleFormat Format,
    int BitsPerSample
);
