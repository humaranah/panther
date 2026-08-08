using System.Runtime.InteropServices;
using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio;

/// <summary>
/// Converts float32 interleaved PCM between sample rates using miniaudio's built-in resampler.
/// This vendored copy of miniaudio only implements two algorithms: linear (with an optional
/// low-pass filter) and a "custom" hook for plugging in an external library. There is no
/// built-in sinc resampler, so this class always resamples using the linear algorithm - see
/// <see cref="ResolveEffectiveQuality"/> for how that relates to <see cref="ResampleQuality"/>.
/// </summary>
public sealed class MiniAudioResampler : IDisposable
{
    private IntPtr _resampler;

    public int Channels { get; }
    public int SourceSampleRate { get; }
    public int TargetSampleRate { get; }

    public MiniAudioResampler(int channels, int sourceSampleRate, int targetSampleRate)
    {
        if (channels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be positive.");
        }

        if (sourceSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSampleRate), "Sample rate must be positive.");
        }

        if (targetSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSampleRate), "Sample rate must be positive.");
        }

        Channels = channels;
        SourceSampleRate = sourceSampleRate;
        TargetSampleRate = targetSampleRate;

        _resampler = Marshal.AllocHGlobal((int)MiniAudioNative.pa_resampler_sizeof());
        var result = MiniAudioNative.pa_resampler_init(
            (uint)channels, (uint)sourceSampleRate, (uint)targetSampleRate, _resampler);

        if (result != MiniAudioNative.MA_SUCCESS)
        {
            Marshal.FreeHGlobal(_resampler);
            _resampler = IntPtr.Zero;
            throw new InvalidOperationException($"Failed to initialize the resampler (miniaudio result {result}).");
        }
    }

    /// <summary>
    /// Resamples as much of <paramref name="input"/> as needed to fill <paramref name="output"/>.
    /// Both spans are interleaved PCM, sized in samples (frames * <see cref="Channels"/>), not frames.
    /// </summary>
    /// <returns>
    /// The number of whole input frames consumed and output frames produced. Produced may be less
    /// than requested if there wasn't enough input; consumed may be less than available if the
    /// output buffer filled up first. Callers should advance their input cursor by FramesConsumed
    /// and only treat the first FramesProduced frames of <paramref name="output"/> as valid.
    /// </returns>
    public unsafe (int FramesConsumed, int FramesProduced) Process(ReadOnlySpan<float> input, Span<float> output)
    {
        EnsureNotDisposed();

        if (input.Length % Channels != 0)
        {
            throw new ArgumentException($"Input length must be a multiple of {Channels} (the channel count).", nameof(input));
        }

        if (output.Length % Channels != 0)
        {
            throw new ArgumentException($"Output length must be a multiple of {Channels} (the channel count).", nameof(output));
        }

        var framesIn = (ulong)(input.Length / Channels);
        var framesOut = (ulong)(output.Length / Channels);

        fixed (float* pIn = input)
        fixed (float* pOut = output)
        {
            var result = MiniAudioNative.ma_resampler_process_pcm_frames(
                _resampler, (IntPtr)pIn, ref framesIn, (IntPtr)pOut, ref framesOut);
            ThrowIfFailed(result, "Failed to resample audio frames");
        }

        return ((int)framesIn, (int)framesOut);
    }

    /// <summary>
    /// How many input frames are needed to produce <paramref name="outputFrameCount"/> output
    /// frames, not counting frames already cached internally. Useful for sizing a read from the
    /// decoder before calling <see cref="Process"/>.
    /// </summary>
    public int GetRequiredInputFrameCount(int outputFrameCount)
    {
        EnsureNotDisposed();
        var result = MiniAudioNative.ma_resampler_get_required_input_frame_count(
            _resampler, (ulong)outputFrameCount, out var inputFrameCount);
        ThrowIfFailed(result, "Failed to compute the required input frame count");
        return (int)inputFrameCount;
    }

    /// <summary>
    /// How many output frames would result from fully consuming <paramref name="inputFrameCount"/>
    /// input frames. Useful for sizing the output buffer before calling <see cref="Process"/>.
    /// </summary>
    public int GetExpectedOutputFrameCount(int inputFrameCount)
    {
        EnsureNotDisposed();
        var result = MiniAudioNative.ma_resampler_get_expected_output_frame_count(
            _resampler, (ulong)inputFrameCount, out var outputFrameCount);
        ThrowIfFailed(result, "Failed to compute the expected output frame count");
        return (int)outputFrameCount;
    }

    /// <summary>
    /// Clears the resampler's internal filter/cache state and resets its timer, e.g. after a seek.
    /// </summary>
    public void Reset()
    {
        EnsureNotDisposed();
        var result = MiniAudioNative.ma_resampler_reset(_resampler);
        ThrowIfFailed(result, "Failed to reset the resampler");
    }

    public void Dispose()
    {
        if (_resampler != IntPtr.Zero)
        {
            MiniAudioNative.ma_resampler_uninit(_resampler, IntPtr.Zero);
            Marshal.FreeHGlobal(_resampler);
            _resampler = IntPtr.Zero;
        }
    }

    /// <summary>
    /// This vendored miniaudio only backs the Linear algorithm. None (rates already match, no
    /// resampler needed) passes through unchanged; every other requested quality - including
    /// Sinc16/32/64 - resolves to Linear until a real sinc backend is wired in (see
    /// PlaybackSettings.ResampleQuality). Callers that care about surfacing this to the user
    /// should compare the result against the requested value.
    /// </summary>
    public static ResampleQuality ResolveEffectiveQuality(ResampleQuality requested) =>
        requested == ResampleQuality.None ? ResampleQuality.None : ResampleQuality.Linear;

    private void EnsureNotDisposed()
    {
        if (_resampler == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(MiniAudioResampler));
        }
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != MiniAudioNative.MA_SUCCESS)
        {
            throw new InvalidOperationException($"{message} (miniaudio result {result}).");
        }
    }
}
