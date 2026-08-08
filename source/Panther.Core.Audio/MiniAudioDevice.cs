using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Panther.Core.Audio.Abstractions;

namespace Panther.Core.Audio;

/// <summary>
/// A playback device backed by miniaudio. Always requests float32 samples from the backend
/// (matching the decoders' output), and always sets wasapi.noAutoConvertSRC so WASAPI never
/// silently resamples a stream out from under an exclusive-mode request.
/// </summary>
public sealed class MiniAudioDevice : IAudioDevice
{
    private static readonly object CallbackRegistrationLock = new();
    private static bool _callbackRegistered;

    private IntPtr _context;
    private IntPtr _device;
    private GCHandle _selfHandle;
    private int _channels;
    private volatile bool _stoppingDeliberately;

    public event Action<Memory<float>, int>? RequestData;
    public event EventHandler? Disconnected;

    /// <summary>
    /// The format the backend actually negotiated after <see cref="Configure"/>, which can
    /// differ from the requested format - most notably in WASAPI exclusive mode, where the
    /// backend always uses the endpoint's current native format/rate rather than ours.
    /// </summary>
    public AudioFormat? NegotiatedFormat { get; private set; }

    public MiniAudioDevice()
    {
        EnsureNativeCallbackRegistered();
        InitContext();
    }

    public DeviceCapabilities GetCapabilities()
    {
        var result = MiniAudioNative.pa_context_get_default_device_info(
            _context, MiniAudioNative.MA_DEVICE_TYPE_PLAYBACK, out var info);
        ThrowIfFailed(result, "Failed to query the default playback device");

        return BuildCapabilities(info);
    }

    /// <summary>
    /// Opens the playback device. When <paramref name="exclusiveMode"/> is true and the backend
    /// negotiates a native sample rate different from <paramref name="format"/>.SampleRate, this
    /// throws instead of silently letting miniaudio's internal converter resample the stream -
    /// that would defeat the point of asking for an exclusive, bit-perfect stream. Callers
    /// implementing a "prefer exclusive" policy should catch the failure and retry with
    /// exclusiveMode: false.
    /// </summary>
    public void Configure(AudioFormat format, bool exclusiveMode)
    {
        if (_device != IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "The device has already been configured. Dispose and create a new instance to reconfigure.");
        }

        _channels = format.Channels;
        _selfHandle = GCHandle.Alloc(this);
        _device = Marshal.AllocHGlobal((int)MiniAudioNative.pa_device_sizeof());

        var shareMode = exclusiveMode ? MiniAudioNative.MA_SHARE_MODE_EXCLUSIVE : MiniAudioNative.MA_SHARE_MODE_SHARED;
        var result = MiniAudioNative.pa_device_init(
            _context, MiniAudioNative.MA_FORMAT_F32, (uint)format.Channels, (uint)format.SampleRate,
            shareMode, GCHandle.ToIntPtr(_selfHandle), _device);

        if (result != MiniAudioNative.MA_SUCCESS)
        {
            FreeDevice();

            var reason = exclusiveMode
                ? $"the driver may not support {format.SampleRate} Hz / {format.Channels}ch natively in exclusive mode"
                : "the requested format may not be supported";
            throw new InvalidOperationException($"Failed to open the playback device (miniaudio result {result}); {reason}.");
        }

        MiniAudioNative.pa_device_get_actual_format(_device, out var actualFormat, out var actualChannels, out var actualSampleRate);
        NegotiatedFormat = new AudioFormat((int)actualSampleRate, (int)actualChannels, format.Format, format.BitsPerSample);

        if (exclusiveMode && (actualSampleRate != (uint)format.SampleRate || actualChannels != (uint)format.Channels))
        {
            var negotiated = NegotiatedFormat.Value;
            FreeDevice();
            throw new InvalidOperationException(
                $"Exclusive mode opened at {negotiated.SampleRate} Hz / {negotiated.Channels}ch instead of the requested " +
                $"{format.SampleRate} Hz / {format.Channels}ch. Refusing to continue, since miniaudio would silently " +
                "rate-convert and/or channel-mix under the hood, defeating bit-perfect exclusive playback. " +
                "Change the Windows default format for this device to match, or fall back to shared mode.");
        }
    }

    public void Start()
    {
        EnsureConfigured();
        var result = MiniAudioNative.ma_device_start(_device);
        ThrowIfFailed(result, "Failed to start the playback device");
    }

    public void Stop()
    {
        EnsureConfigured();
        _stoppingDeliberately = true;
        try
        {
            MiniAudioNative.ma_device_stop(_device);
        }
        finally
        {
            _stoppingDeliberately = false;
        }
    }

    public void Dispose()
    {
        FreeDevice();

        if (_context != IntPtr.Zero)
        {
            MiniAudioNative.ma_context_uninit(_context);
            Marshal.FreeHGlobal(_context);
            _context = IntPtr.Zero;
        }
    }

    private void InitContext()
    {
        _context = Marshal.AllocHGlobal((int)MiniAudioNative.ma_context_sizeof());
        var result = MiniAudioNative.ma_context_init(IntPtr.Zero, 0, IntPtr.Zero, _context);
        if (result != MiniAudioNative.MA_SUCCESS)
        {
            Marshal.FreeHGlobal(_context);
            _context = IntPtr.Zero;
            throw new InvalidOperationException($"Failed to initialize the audio context (miniaudio result {result}).");
        }
    }

    private void FreeDevice()
    {
        if (_device != IntPtr.Zero)
        {
            MiniAudioNative.ma_device_uninit(_device);
            Marshal.FreeHGlobal(_device);
            _device = IntPtr.Zero;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }

        NegotiatedFormat = null;
    }

    private void EnsureConfigured()
    {
        if (_device == IntPtr.Zero)
        {
            throw new InvalidOperationException("Configure() must be called before starting or stopping the device.");
        }
    }

    private unsafe void HandleDataCallback(IntPtr output, uint frameCount)
    {
        var handler = RequestData;
        var sampleCount = checked((int)frameCount * _channels);
        var outputSpan = new Span<float>((void*)output, sampleCount);

        if (handler is null)
        {
            outputSpan.Clear();
            return;
        }

        var buffer = ArrayPool<float>.Shared.Rent(sampleCount);
        try
        {
            handler(new Memory<float>(buffer, 0, sampleCount), (int)frameCount);
            buffer.AsSpan(0, sampleCount).CopyTo(outputSpan);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Fires when miniaudio reports the device stopped for a reason other than us calling
    /// <see cref="Stop"/> ourselves - most likely the output device was unplugged or a driver
    /// error occurred. Dispatched off the notification thread since miniaudio's docs say not to
    /// do anything heavy (or touch the device) from directly within the callback.
    /// </summary>
    private void HandleNotification(int type)
    {
        if (type != MiniAudioNative.MA_DEVICE_NOTIFICATION_TYPE_STOPPED || _stoppingDeliberately)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(_ => Disconnected?.Invoke(this, EventArgs.Empty));
    }

    private static void EnsureNativeCallbackRegistered()
    {
        if (_callbackRegistered)
        {
            return;
        }

        lock (CallbackRegistrationLock)
        {
            if (_callbackRegistered)
            {
                return;
            }

            unsafe
            {
                delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, uint, void> dataCallback = &NativeDataCallback;
                MiniAudioNative.pa_device_set_data_callback((IntPtr)dataCallback);

                delegate* unmanaged[Cdecl]<IntPtr, int, void> notificationCallback = &NativeNotificationCallback;
                MiniAudioNative.pa_device_set_notification_callback((IntPtr)notificationCallback);
            }

            _callbackRegistered = true;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeDataCallback(IntPtr userData, IntPtr output, IntPtr input, uint frameCount)
    {
        if (userData == IntPtr.Zero)
        {
            return;
        }

        if (GCHandle.FromIntPtr(userData).Target is MiniAudioDevice device)
        {
            device.HandleDataCallback(output, frameCount);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeNotificationCallback(IntPtr userData, int type)
    {
        if (userData == IntPtr.Zero)
        {
            return;
        }

        if (GCHandle.FromIntPtr(userData).Target is MiniAudioDevice device)
        {
            device.HandleNotification(type);
        }
    }

    private static DeviceCapabilities BuildCapabilities(MiniAudioNative.DeviceInfoNative info)
    {
        var sampleRates = new List<int>();
        var minBits = int.MaxValue;
        var maxBits = 0;
        var maxChannels = 0;
        var supportsExclusive = false;

        for (var i = 0; i < info.nativeDataFormatCount; i++)
        {
            var entry = info.nativeDataFormats[i];

            if (entry.sampleRate > 0 && !sampleRates.Contains((int)entry.sampleRate))
            {
                sampleRates.Add((int)entry.sampleRate);
            }

            if (entry.channels > maxChannels)
            {
                maxChannels = (int)entry.channels;
            }

            var bits = MapBitsPerSample(entry.format);
            if (bits > 0)
            {
                minBits = Math.Min(minBits, bits);
                maxBits = Math.Max(maxBits, bits);
            }

            if ((entry.flags & MiniAudioNative.MA_DATA_FORMAT_FLAG_EXCLUSIVE_MODE) != 0)
            {
                supportsExclusive = true;
            }
        }

        sampleRates.Sort();

        return new DeviceCapabilities(
            sampleRates.ToArray(),
            minBits == int.MaxValue ? 0 : minBits,
            maxBits,
            maxChannels,
            supportsExclusive);
    }

    private static int MapBitsPerSample(int maFormat) => maFormat switch
    {
        MiniAudioNative.MA_FORMAT_U8 => 8,
        MiniAudioNative.MA_FORMAT_S16 => 16,
        MiniAudioNative.MA_FORMAT_S24 => 24,
        MiniAudioNative.MA_FORMAT_S32 => 32,
        MiniAudioNative.MA_FORMAT_F32 => 32,
        _ => 0
    };

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != MiniAudioNative.MA_SUCCESS)
        {
            throw new InvalidOperationException($"{message} (miniaudio result {result}).");
        }
    }
}
