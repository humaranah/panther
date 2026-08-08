namespace Panther.Core.Audio.Abstractions;

/// <summary>
/// Represents an audio decoder that can decode audio data from a stream.
/// </summary>
public interface IAudioDecoder : IDisposable
{
    /// <summary>
    /// Probes the audio format of the given stream.
    /// </summary>
    /// <param name="source">The stream containing the audio data.</param>
    /// <returns>The audio format of the stream.</returns>
    AudioFormat Probe(Stream source);

    /// <summary>
    /// Opens the audio decoder with the given stream and audio format.
    /// </summary>
    /// <param name="source">The stream containing the audio data.</param>
    /// <param name="format">The audio format of the stream.</param>
    void Open(Stream source, AudioFormat format);

    /// <summary>
    /// Reads a specified number of audio frames into the provided buffer.
    /// </summary>
    /// <param name="buffer">The buffer to store the audio frames.</param>
    /// <param name="frameCount">The number of frames to read.</param>
    /// <returns>The number of frames actually read.</returns>
    int ReadFrames(Span<float> buffer, int frameCount);

    /// <summary>
    /// Gets the total number of frames in the audio stream.
    /// </summary>
    long TotalFrames { get; }

    /// <summary>
    /// Gets the current position in the audio stream.
    /// </summary>
    long CurrentFrame { get; }

    /// <summary>
    /// Gets a value indicating whether the audio decoder supports seeking.
    /// </summary>
    bool SupportsSeek { get; }

    /// <summary>
    /// Seeks to the specified frame in the audio stream.
    /// </summary>
    /// <param name="frame">The frame to seek to.</param>
    void Seek(long frame);
}
