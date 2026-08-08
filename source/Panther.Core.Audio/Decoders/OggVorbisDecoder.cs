using Panther.Core.Audio.Abstractions;

namespace Panther.Core.Audio.Decoders;

public class OggVorbisDecoder : IAudioDecoder
{
    public long TotalFrames => throw new NotImplementedException();

    public long CurrentFrame => throw new NotImplementedException();

    public bool SupportsSeek => throw new NotImplementedException();

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Open(Stream source, AudioFormat format)
    {
        throw new NotImplementedException();
    }

    public AudioFormat Probe(Stream source)
    {
        throw new NotImplementedException();
    }

    public int ReadFrames(Span<float> buffer, int frameCount)
    {
        throw new NotImplementedException();
    }

    public void Seek(long frame)
    {
        throw new NotImplementedException();
    }
}
