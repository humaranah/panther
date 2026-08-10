namespace Panther.Core.Audio.Decoders;

internal class FlacDecoder : MiniAudioDecoderBase
{
    protected override int EncodingFormat => MiniAudioNative.MA_ENCODING_FORMAT_FLAC;
}
