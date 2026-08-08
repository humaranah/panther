using Panther.Core.Audio.Abstractions;
using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio.Tests.Fakes;

/// <summary>
/// Decodes silence for the first <see cref="FailAfterReads"/> ReadFrames calls, then throws -
/// simulates a mid-stream decode error, which is impractical to trigger with a real (well-formed)
/// audio file.
/// </summary>
internal sealed class ThrowingAudioDecoder(AudioFormat format, int failAfterReads = 1) : IAudioDecoder
{
    private int _readCount;
    private long _currentFrame;

    public long TotalFrames => 44100;
    public long CurrentFrame => _currentFrame;
    public bool SupportsSeek => false;

    public AudioFormat Probe(Stream source) => format;

    public void Open(Stream source, AudioFormat openFormat)
    {
    }

    public int ReadFrames(Span<float> buffer, int frameCount)
    {
        if (++_readCount > failAfterReads)
        {
            throw new InvalidOperationException("Simulated decode failure.");
        }

        buffer[..(frameCount * format.Channels)].Clear();
        _currentFrame += frameCount;
        return frameCount;
    }

    public void Seek(long frame) => throw new NotSupportedException();

    public void Dispose()
    {
    }
}

internal sealed class ThrowingAudioDecoderFactory(AudioFormat format, int failAfterReads = 1) : IAudioDecoderFactory
{
    public IAudioDecoder CreateFor(Stream source, string fileName) => new ThrowingAudioDecoder(format, failAfterReads);
}
