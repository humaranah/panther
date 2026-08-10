#define MA_DLL
#define MINIAUDIO_IMPLEMENTATION
#include "miniaudio.h"

/*
miniaudio exposes ma_context_sizeof() but has no equivalent for ma_device, whose
layout is opaque to callers. Panther.Core.Audio needs the real size to allocate a
buffer before calling ma_device_init(), so we export it ourselves here, compiled
against the exact same struct definition as the rest of this translation unit.
*/
MA_API size_t pa_device_sizeof(void)
{
    return sizeof(ma_device);
}

/*
Same rationale as pa_device_sizeof(): ma_decoder's layout is opaque to callers,
so Panther.Core.Audio needs the real size to allocate a buffer before calling
ma_decoder_init_memory().
*/
MA_API size_t pa_decoder_sizeof(void)
{
    return sizeof(ma_decoder);
}

/*
ma_decoder_config has a complex layout (nested resampler config, allocation
callbacks, custom backend vtables, ...) that isn't worth replicating on the C#
side just to call ma_decoder_init_memory(). This wrapper builds the config from
primitive arguments instead:
  - encodingFormat: an ma_encoding_format value, so the caller can tell miniaudio
    exactly which container/codec to expect instead of relying on sniffing.
  - outputFormat: ma_format_unknown (0) to decode in the stream's native sample
    format (used for probing), or ma_format_f32 (5) to force float32 output for
    actual playback reads.
Decoding from memory requires pData to remain valid for the lifetime of the
decoder (miniaudio reads from it lazily on each ma_decoder_read_pcm_frames call),
so the caller must keep the backing buffer pinned until ma_decoder_uninit().
*/
MA_API ma_result pa_decoder_init_memory(const void* pData, size_t dataSize, int encodingFormat, int outputFormat, ma_decoder* pDecoder)
{
    ma_decoder_config config = ma_decoder_config_init((ma_format)outputFormat, 0, 0);
    config.encodingFormat = (ma_encoding_format)encodingFormat;

    return ma_decoder_init_memory(pData, dataSize, &config, pDecoder);
}

/*
ma_context_get_device_info() takes `const ma_device_id*` and accepts NULL to mean
"the default device". LibraryImport can't express a nullable-by-value struct
param against the same signature as the by-ref overload used for a specific
device id, so we expose this dedicated entry point for the "default device"
case, callable with a plain null pointer from C#.
*/
MA_API ma_result pa_context_get_default_device_info(ma_context* pContext, int deviceType, ma_device_info* pDeviceInfo)
{
    return ma_context_get_device_info(pContext, (ma_device_type)deviceType, NULL, pDeviceInfo);
}

/*
Like ma_decoder_config, ma_device_config's layout (per-backend nested structs for
wasapi/alsa/pulse/coreaudio/...) isn't worth replicating in C#. This wrapper
builds it from primitive arguments and always sets wasapi.noAutoConvertSRC so
WASAPI never silently resamples under an exclusive-mode stream, which would
defeat the point of asking for bit-perfect output.

The data callback is routed through a single process-wide trampoline
(pa_device_on_data) registered once via pa_device_set_data_callback(). Per-device
identity is carried through pUserData, which miniaudio stores on our behalf and
hands back to the callback via pDevice->pUserData - the caller is expected to
pass a stable token (e.g. a GCHandle) here rather than a raw object pointer.
*/
typedef void (* pa_data_callback)(void* pUserData, void* pOutput, const void* pInput, ma_uint32 frameCount);

static pa_data_callback g_pa_data_callback = NULL;

MA_API void pa_device_set_data_callback(pa_data_callback callback)
{
    g_pa_data_callback = callback;
}

static void pa_device_on_data(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount)
{
    if (g_pa_data_callback != NULL) {
        g_pa_data_callback(pDevice->pUserData, pOutput, pInput, frameCount);
    }
}

/*
Notifies of device state changes (started/stopped/rerouted/...) - most importantly, this is how
Panther.Core.Audio can tell an unexpected stop (e.g. the output device was unplugged) apart from
one it triggered itself via ma_device_stop(). Same process-wide-trampoline-plus-pUserData pattern
as the data callback; the notification struct itself carries no data beyond pDevice and the type,
so we unpack it here rather than replicating ma_device_notification in C#.
*/
typedef void (* pa_notification_callback)(void* pUserData, int type);

static pa_notification_callback g_pa_notification_callback = NULL;

MA_API void pa_device_set_notification_callback(pa_notification_callback callback)
{
    g_pa_notification_callback = callback;
}

static void pa_device_on_notification(const ma_device_notification* pNotification)
{
    if (g_pa_notification_callback != NULL) {
        g_pa_notification_callback(pNotification->pDevice->pUserData, (int)pNotification->type);
    }
}

MA_API ma_result pa_device_init(ma_context* pContext, int format, ma_uint32 channels, ma_uint32 sampleRate, int shareMode, void* pUserData, ma_device* pDevice)
{
    ma_device_config config = ma_device_config_init(ma_device_type_playback);
    config.playback.format   = (ma_format)format;
    config.playback.channels = channels;
    config.playback.shareMode = (ma_share_mode)shareMode;
    config.sampleRate        = sampleRate;
    config.dataCallback      = pa_device_on_data;
    config.notificationCallback = pa_device_on_notification;
    config.pUserData         = pUserData;
    config.wasapi.noAutoConvertSRC = MA_TRUE;

    return ma_device_init(pContext, &config, pDevice);
}

/*
ma_device has no public getter for the format it actually negotiated with the
backend (pDevice->playback.internal*), which can differ from what was requested
- e.g. in WASAPI exclusive mode, where miniaudio always uses the endpoint's
current native format/rate rather than the one passed into pa_device_init().
Panther.Core.Audio needs this to detect a sample-rate mismatch and refuse to let
miniaudio's internal converter silently resample a stream opened for bit-perfect
exclusive playback.
*/
MA_API void pa_device_get_actual_format(ma_device* pDevice, int* pFormat, ma_uint32* pChannels, ma_uint32* pSampleRate)
{
    *pFormat = (int)pDevice->playback.internalFormat;
    *pChannels = pDevice->playback.internalChannels;
    *pSampleRate = pDevice->playback.internalSampleRate;
}

/*
Same rationale as the decoder/device wrappers above: ma_resampler is opaque, and
ma_resampler_config isn't worth replicating in C# just to call ma_resampler_init().
This project only ever uses miniaudio's built-in linear resampler (its only
non-custom algorithm in this version) at the format the decoders always output.
*/
MA_API size_t pa_resampler_sizeof(void)
{
    return sizeof(ma_resampler);
}

MA_API ma_result pa_resampler_init(ma_uint32 channels, ma_uint32 sampleRateIn, ma_uint32 sampleRateOut, ma_resampler* pResampler)
{
    ma_resampler_config config = ma_resampler_config_init(ma_format_f32, channels, sampleRateIn, sampleRateOut, ma_resample_algorithm_linear);
    return ma_resampler_init(&config, NULL, pResampler);
}
