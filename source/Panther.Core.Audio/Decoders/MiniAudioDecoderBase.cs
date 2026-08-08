using System.Runtime.InteropServices;
using Panther.Core.Audio.Abstractions;
using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio.Decoders;

/// <summary>
/// Base implementation of <see cref="IAudioDecoder"/> backed by miniaudio's built-in
/// ma_decoder, which natively supports WAV, FLAC and MP3. The decoder always outputs
/// float32 samples; the source file's native format is only reported for information
/// purposes via <see cref="Probe"/>.
/// </summary>
internal abstract class MiniAudioDecoderBase : IAudioDecoder
{
    /// <summary>
    /// The container/codec miniaudio should expect (ma_encoding_format), so it decodes
    /// against the known format instead of sniffing the content.
    /// </summary>
    protected abstract int EncodingFormat { get; }

    private byte[]? _fileBytes;
    private GCHandle _pinnedFile;
    private IntPtr _decoder;
    private int _channels;
    private long _totalFrames;

    public long TotalFrames => _totalFrames;

    public long CurrentFrame
    {
        get
        {
            EnsureOpened();
            var result = MiniAudioNative.ma_decoder_get_cursor_in_pcm_frames(_decoder, out var cursor);
            ThrowIfFailed(result, "Failed to read the current decoder position");
            return (long)cursor;
        }
    }

    public bool SupportsSeek => true;

    public AudioFormat Probe(Stream source)
    {
        var bytes = ReadAllBytes(source);
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var decoder = Marshal.AllocHGlobal((int)MiniAudioNative.pa_decoder_sizeof());
        try
        {
            var result = MiniAudioNative.pa_decoder_init_memory(
                handle.AddrOfPinnedObject(), (nuint)bytes.Length,
                EncodingFormat, MiniAudioNative.MA_FORMAT_UNKNOWN, decoder);
            ThrowIfFailed(result, "Failed to probe the audio stream");

            var formatResult = MiniAudioNative.ma_decoder_get_data_format(
                decoder, out var format, out var channels, out var sampleRate, IntPtr.Zero, 0);
            ThrowIfFailed(formatResult, "Failed to read the audio stream's format");

            MiniAudioNative.ma_decoder_uninit(decoder);

            var (sampleFormat, bitsPerSample) = MapFormat(format);
            return new AudioFormat((int)sampleRate, (int)channels, sampleFormat, bitsPerSample);
        }
        finally
        {
            Marshal.FreeHGlobal(decoder);
            handle.Free();
        }
    }

    public void Open(Stream source, AudioFormat format)
    {
        if (_decoder != IntPtr.Zero)
        {
            throw new InvalidOperationException("The decoder has already been opened.");
        }

        _fileBytes = ReadAllBytes(source);
        _pinnedFile = GCHandle.Alloc(_fileBytes, GCHandleType.Pinned);
        _decoder = Marshal.AllocHGlobal((int)MiniAudioNative.pa_decoder_sizeof());

        var result = MiniAudioNative.pa_decoder_init_memory(
            _pinnedFile.AddrOfPinnedObject(), (nuint)_fileBytes.Length,
            EncodingFormat, MiniAudioNative.MA_FORMAT_F32, _decoder);
        ThrowIfFailed(result, "Failed to open the audio stream");

        _channels = format.Channels;

        var lengthResult = MiniAudioNative.ma_decoder_get_length_in_pcm_frames(_decoder, out var length);
        ThrowIfFailed(lengthResult, "Failed to read the audio stream's length");
        _totalFrames = (long)length;
    }

    public int ReadFrames(Span<float> buffer, int frameCount)
    {
        EnsureOpened();

        var requiredLength = (long)frameCount * _channels;
        if (buffer.Length < requiredLength)
        {
            throw new ArgumentException(
                $"Buffer must be at least {requiredLength} floats long ({frameCount} frames * {_channels} channels).",
                nameof(buffer));
        }

        ulong framesRead;
        unsafe
        {
            fixed (float* pBuffer = buffer)
            {
                var result = MiniAudioNative.ma_decoder_read_pcm_frames(
                    _decoder, (IntPtr)pBuffer, (ulong)frameCount, out framesRead);
                if (result != MiniAudioNative.MA_SUCCESS && result != MiniAudioNative.MA_AT_END)
                {
                    ThrowIfFailed(result, "Failed to read audio frames");
                }
            }
        }

        return (int)framesRead;
    }

    public void Seek(long frame)
    {
        EnsureOpened();
        var result = MiniAudioNative.ma_decoder_seek_to_pcm_frame(_decoder, (ulong)frame);
        ThrowIfFailed(result, "Failed to seek the audio stream");
    }

    public void Dispose()
    {
        if (_decoder != IntPtr.Zero)
        {
            MiniAudioNative.ma_decoder_uninit(_decoder);
            Marshal.FreeHGlobal(_decoder);
            _decoder = IntPtr.Zero;
        }

        if (_pinnedFile.IsAllocated)
        {
            _pinnedFile.Free();
        }

        _fileBytes = null;
    }

    private void EnsureOpened()
    {
        if (_decoder == IntPtr.Zero)
        {
            throw new InvalidOperationException("The decoder has not been opened yet.");
        }
    }

    private static byte[] ReadAllBytes(Stream source)
    {
        if (source.CanSeek)
        {
            source.Seek(0, SeekOrigin.Begin);
        }

        using var memoryStream = new MemoryStream();
        source.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static (SampleFormat Format, int BitsPerSample) MapFormat(int maFormat) => maFormat switch
    {
        MiniAudioNative.MA_FORMAT_U8 => (SampleFormat.Int16, 8),
        MiniAudioNative.MA_FORMAT_S16 => (SampleFormat.Int16, 16),
        MiniAudioNative.MA_FORMAT_S24 => (SampleFormat.Int24, 24),
        MiniAudioNative.MA_FORMAT_S32 => (SampleFormat.Int32, 32),
        MiniAudioNative.MA_FORMAT_F32 => (SampleFormat.Float32, 32),
        _ => throw new NotSupportedException($"Unsupported miniaudio sample format ({maFormat}).")
    };

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != MiniAudioNative.MA_SUCCESS)
        {
            throw new InvalidOperationException($"{message} (miniaudio result {result}).");
        }
    }
}
