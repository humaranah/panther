namespace Panther.Core.Audio;

/// <summary>
/// A file abstraction for TagLib that uses a provided stream for reading audio data.
/// </summary>
/// <param name="name">The name of the file.</param>
/// <param name="stream">The stream to read audio data from.</param>
public sealed class StreamFileAbstraction(
    string name,
    Stream stream) : TagLib.File.IFileAbstraction
{
    /// <summary>
    /// Gets the name of the file abstraction.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the stream used for reading audio data.
    /// </summary>
    public Stream ReadStream { get; } = stream;

    /// <summary>
    /// Gets the stream used for writing audio data.
    /// This implementation does not support writing, so it throws a NotSupportedException.
    /// </summary>
    public Stream WriteStream => throw new NotSupportedException("Write stream is not supported.");

    /// <summary>
    /// Closes the provided stream.
    /// In this implementation, no action is taken since the stream is managed externally.
    /// </summary>
    /// <param name="stream">The stream to close.</param>
    public void CloseStream(Stream stream)
    {
        // No action needed, as the stream is managed externally.
    }
}
