namespace Panther.Core.Audio.Abstractions;

/// <summary>
/// Represents a factory for creating audio decoders.
/// </summary>
public interface IAudioDecoderFactory
{
    /// <summary>
    /// Creates an audio decoder for the specified source stream.
    /// </summary>
    /// <param name="source">The source stream to decode.</param>
    /// <returns>An audio decoder for the specified source stream.</returns>
    IAudioDecoder CreateFor(Stream source, string fileName);
}
