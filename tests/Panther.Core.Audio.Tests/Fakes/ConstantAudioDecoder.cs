using Panther.Core.Audio.Abstractions;

namespace Panther.Core.Audio.Tests.Fakes;

/// <summary>
/// Decodes a fixed sample value forever (up to <paramref name="totalFrames"/>). Used to verify the
/// exact shape of AudioPlayer's fade/volume gain envelope: since every input sample is identical,
/// whatever comes out of a Pump() *is* the gain that was applied, with no signal shape to account
/// for.
/// </summary>
internal sealed class ConstantAudioDecoder(AudioFormat format, float value = 1f, long totalFrames = 1_000_000) : IAudioDecoder
{
    private long _currentFrame;

    public long TotalFrames => totalFrames;
    public long CurrentFrame => _currentFrame;
    public bool SupportsSeek => true;

    public AudioFormat Probe(Stream source) => format;

    public void Open(Stream source, AudioFormat openFormat)
    {
    }

    public int ReadFrames(Span<float> buffer, int frameCount)
    {
        var framesAvailable = (int)Math.Min(frameCount, totalFrames - _currentFrame);
        if (framesAvailable <= 0)
        {
            return 0;
        }

        buffer[..(framesAvailable * format.Channels)].Fill(value);
        _currentFrame += framesAvailable;
        return framesAvailable;
    }

    public void Seek(long frame) => _currentFrame = frame;

    public void Dispose()
    {
    }
}

internal sealed class ConstantAudioDecoderFactory(AudioFormat format, float value = 1f, long totalFrames = 1_000_000) : IAudioDecoderFactory
{
    public IAudioDecoder CreateFor(Stream source, string fileName) => new ConstantAudioDecoder(format, value, totalFrames);
}
