using Panther.Core.Audio.Abstractions;
using Panther.Core.Audio.Decoders;

namespace Panther.Core.Audio;

public class AudioDecoderFactory : IAudioDecoderFactory
{
    public IAudioDecoder CreateFor(Stream source, string fileName)
    {
        var abstraction = new StreamFileAbstraction(fileName, source);
        using var tagFile = TagLib.File.Create(abstraction);

        return tagFile switch
        {
            TagLib.Flac.File => new FlacDecoder(),
            TagLib.Mpeg.AudioFile => new Mp3Decoder(),
            TagLib.Ogg.File => new OggVorbisDecoder(),
            TagLib.Riff.File => new WavDecoder(),
            TagLib.Mpeg4.File mp4File => ResolveMpeg4Codec(mp4File),
            _ => throw new NotSupportedException($"The audio format of the file '{fileName}' is not supported.")
        };
    }

    private static IAudioDecoder ResolveMpeg4Codec(TagLib.Mpeg4.File mp4File)
    {
        var audioCodec = mp4File.Properties.Codecs
            .FirstOrDefault(codec => (codec.MediaTypes & TagLib.MediaTypes.Audio) != 0);

        return audioCodec?.Description switch
        {
            var d when d?.Contains("Apple Lossless", StringComparison.OrdinalIgnoreCase) == true => new AlacDecoder(),
            //var d when d?.Contains("AAC", StringComparison.OrdinalIgnoreCase) == true => new AacDecoder(),
            _ => throw new NotSupportedException("The MPEG-4 audio codec is not supported.")
        };
    }
}
