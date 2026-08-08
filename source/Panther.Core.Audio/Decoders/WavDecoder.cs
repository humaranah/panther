namespace Panther.Core.Audio.Decoders;

internal class WavDecoder : MiniAudioDecoderBase
{
    protected override int EncodingFormat => MiniAudioNative.MA_ENCODING_FORMAT_WAV;
}
