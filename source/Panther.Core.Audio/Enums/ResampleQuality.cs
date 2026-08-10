namespace Panther.Core.Audio.Enums;

public enum ResampleQuality
{
    None,    // No resampling, just copy the samples as they are
    Linear,  // Linear interpolation, fast but low quality
    Sinc16,  // Sinc interpolation with 16 taps, good quality
    Sinc32,  // Sinc interpolation with 32 taps, better quality
    Sinc64   // Sinc interpolation with 64 taps, best quality
}
