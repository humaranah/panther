namespace Panther.Core.Audio.Decoders;

internal class Mp3Decoder : MiniAudioDecoderBase
{
    protected override int EncodingFormat => MiniAudioNative.MA_ENCODING_FORMAT_MP3;
}
