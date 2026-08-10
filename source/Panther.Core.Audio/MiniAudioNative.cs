using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Panther.Core.Audio;

public static partial class MiniAudioNative
{
    private const string LibraryName = "miniaudio";

    /// <summary>
    /// Matches pa_data_callback in miniaudio_impl.c. userData is whatever token was passed as
    /// pUserData to pa_device_init (a GCHandle in this project's usage).
    /// </summary>
    public unsafe delegate void DataCallback(IntPtr userData, IntPtr output, IntPtr input, uint frameCount);

    #region Extern Functions
    // ma_device has no public sizeof() in miniaudio's API (unlike ma_context); pa_device_sizeof
    // is a small helper we export ourselves from native/src/miniaudio_impl.c.
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint pa_device_sizeof();

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint ma_context_sizeof();

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_context_init(IntPtr backends, uint backendCount,
        IntPtr config, IntPtr context);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_context_uninit(IntPtr context);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int pa_device_init(IntPtr context,
        int format, uint channels, uint sampleRate, int shareMode, IntPtr userData, IntPtr device);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pa_device_set_data_callback(IntPtr callback);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pa_device_set_notification_callback(IntPtr callback);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void pa_device_get_actual_format(IntPtr device,
        out int format, out uint channels, out uint sampleRate);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_device_start(IntPtr device);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void ma_device_stop(IntPtr device);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void ma_device_uninit(IntPtr device);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_context_get_devices(IntPtr context,
        out IntPtr pPlaybackInfos, out uint playbackCount,
        out IntPtr pCaptureInfos, out uint captureCount);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_context_get_device_info(IntPtr context,
        uint deviceType, ref DeviceIdNative id, out DeviceInfoNative info);

    // ma_context_get_device_info() accepts a null device id to mean "the default device", but
    // LibraryImport can't express that against the by-ref overload above, so this dedicated
    // wrapper (see miniaudio_impl.c) covers the default-device case.
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int pa_context_get_default_device_info(IntPtr context,
        int deviceType, out DeviceInfoNative info);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint pa_decoder_sizeof();

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int pa_decoder_init_memory(IntPtr data, nuint dataSize,
        int encodingFormat, int outputFormat, IntPtr decoder);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_decoder_uninit(IntPtr decoder);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_decoder_read_pcm_frames(IntPtr decoder, IntPtr framesOut,
        ulong frameCount, out ulong framesRead);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_decoder_seek_to_pcm_frame(IntPtr decoder, ulong frameIndex);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_decoder_get_data_format(IntPtr decoder,
        out int format, out uint channels, out uint sampleRate,
        IntPtr channelMap, nuint channelMapCap);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_decoder_get_cursor_in_pcm_frames(IntPtr decoder, out ulong cursor);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_decoder_get_length_in_pcm_frames(IntPtr decoder, out ulong length);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nuint pa_resampler_sizeof();

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int pa_resampler_init(uint channels, uint sampleRateIn, uint sampleRateOut, IntPtr resampler);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void ma_resampler_uninit(IntPtr resampler, IntPtr allocationCallbacks);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_resampler_process_pcm_frames(IntPtr resampler,
        IntPtr framesIn, ref ulong frameCountIn, IntPtr framesOut, ref ulong frameCountOut);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_resampler_get_required_input_frame_count(IntPtr resampler,
        ulong outputFrameCount, out ulong inputFrameCount);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_resampler_get_expected_output_frame_count(IntPtr resampler,
        ulong inputFrameCount, out ulong outputFrameCount);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int ma_resampler_reset(IntPtr resampler);
    #endregion

    #region Decoder Constants
    // Mirrors ma_format: the sample format decoded PCM frames are stored in.
    public const int MA_FORMAT_UNKNOWN = 0;
    public const int MA_FORMAT_U8 = 1;
    public const int MA_FORMAT_S16 = 2;
    public const int MA_FORMAT_S24 = 3;
    public const int MA_FORMAT_S32 = 4;
    public const int MA_FORMAT_F32 = 5;

    // Mirrors ma_encoding_format: the container/codec a decoder should expect.
    public const int MA_ENCODING_FORMAT_WAV = 1;
    public const int MA_ENCODING_FORMAT_FLAC = 2;
    public const int MA_ENCODING_FORMAT_MP3 = 3;

    // Mirrors the ma_result values this project's decoder wrapper relies on.
    public const int MA_SUCCESS = 0;
    public const int MA_AT_END = -17;

    // Mirrors ma_device_type. pa_device_init() only ever opens a playback device.
    public const int MA_DEVICE_TYPE_PLAYBACK = 1;

    // Mirrors ma_share_mode.
    public const int MA_SHARE_MODE_SHARED = 0;
    public const int MA_SHARE_MODE_EXCLUSIVE = 1;

    // Mirrors MA_DATA_FORMAT_FLAG_EXCLUSIVE_MODE: set on a NativeDataFormat entry that's
    // natively supported in exclusive mode.
    public const uint MA_DATA_FORMAT_FLAG_EXCLUSIVE_MODE = 1U << 1;

    // Mirrors ma_device_notification_type.
    public const int MA_DEVICE_NOTIFICATION_TYPE_STARTED = 0;
    public const int MA_DEVICE_NOTIFICATION_TYPE_STOPPED = 1;
    public const int MA_DEVICE_NOTIFICATION_TYPE_REROUTED = 2;
    public const int MA_DEVICE_NOTIFICATION_TYPE_INTERRUPTION_BEGAN = 3;
    public const int MA_DEVICE_NOTIFICATION_TYPE_INTERRUPTION_ENDED = 4;
    public const int MA_DEVICE_NOTIFICATION_TYPE_UNLOCKED = 5;
    #endregion

    #region Structs
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DeviceIdNative
    {
        public fixed byte raw[256];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeDataFormat
    {
        public int format;
        public uint channels;
        public uint sampleRate;
        public uint flags;
    }

    // Matches the 64-element inline array embedded in ma_device_info.nativeDataFormats.
    // ma_device_info stores this inline (not as a pointer), so a plain fixed buffer won't
    // work here since NativeDataFormat isn't a primitive type; InlineArray gives us the
    // equivalent blittable layout.
    [InlineArray(64)]
    public struct NativeDataFormatArray
    {
        private NativeDataFormat _element0;
    }

    // Mirrors miniaudio's single ma_device_info struct, used both for enumeration
    // (ma_context_get_devices) and detailed lookups (ma_context_get_device_info).
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DeviceInfoNative
    {
        public DeviceIdNative id;
        public fixed byte name[256];
        public int isDefault;
        public uint nativeDataFormatCount;
        public NativeDataFormatArray nativeDataFormats;

        public string GetName()
        {
            fixed (byte* p = name)
            {
                return GetFixedString(p, 256);
            }
        }
    }
    #endregion

    private static unsafe string GetFixedString(byte* ptr, int maxLength)
    {
        int len = 0;
        while (len < maxLength && ptr[len] != 0)
        {
            len++;
        }

        return System.Text.Encoding.UTF8.GetString(ptr, len);
    }
}
