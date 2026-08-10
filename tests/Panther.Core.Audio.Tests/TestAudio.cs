namespace Panther.Core.Audio.Tests;

/// <summary>
/// Generates synthetic PCM WAV data for tests, and small signal-analysis helpers to assert on it,
/// so decoder/resampler/player tests can verify actual sample correctness instead of just "it
/// didn't throw".
/// </summary>
internal static class TestAudio
{
    public static byte[] BuildSineWaveWav(int sampleRate, int channels, int bitsPerSample, int frameCount, double freqHz, double amplitude = 0.5)
    {
        var bytesPerSample = bitsPerSample / 8;
        var dataSize = frameCount * channels * bytesPerSample;
        var byteRate = sampleRate * channels * bytesPerSample;
        var blockAlign = (short)(channels * bytesPerSample);

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write("RIFF"u8.ToArray());
        w.Write(36 + dataSize);
        w.Write("WAVE"u8.ToArray());

        w.Write("fmt "u8.ToArray());
        w.Write(16);
        w.Write((short)1); // PCM
        w.Write((short)channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write(blockAlign);
        w.Write((short)bitsPerSample);

        w.Write("data"u8.ToArray());
        w.Write(dataSize);

        for (var frame = 0; frame < frameCount; frame++)
        {
            var t = frame / (double)sampleRate;
            var value = Math.Sin(2 * Math.PI * freqHz * t) * amplitude;

            for (var ch = 0; ch < channels; ch++)
            {
                WriteSample(w, bitsPerSample, value);
            }
        }

        w.Flush();
        return ms.ToArray();
    }

    private static void WriteSample(BinaryWriter w, int bitsPerSample, double value)
    {
        switch (bitsPerSample)
        {
            case 8:
                // WAV 8-bit PCM is unsigned, centered on 128.
                w.Write((byte)Math.Round((value * 127.0) + 128.0));
                break;
            case 16:
                w.Write((short)Math.Round(value * short.MaxValue));
                break;
            case 24:
                var s24 = (int)Math.Round(value * 8388607.0);
                w.Write((byte)(s24 & 0xFF));
                w.Write((byte)((s24 >> 8) & 0xFF));
                w.Write((byte)((s24 >> 16) & 0xFF));
                break;
            case 32:
                w.Write((int)Math.Round(value * int.MaxValue));
                break;
            default:
                throw new NotSupportedException($"Unsupported bits per sample for the test WAV generator: {bitsPerSample}");
        }
    }

    /// <summary>Writes a synthetic sine WAV to a fresh temp file and returns its path.</summary>
    public static string WriteTempSineWav(int sampleRate, int channels, int bitsPerSample, int frameCount, double freqHz, double amplitude = 0.5)
    {
        var path = Path.Combine(Path.GetTempPath(), $"panther-audio-test-{Guid.NewGuid():N}.wav");
        File.WriteAllBytes(path, BuildSineWaveWav(sampleRate, channels, bitsPerSample, frameCount, freqHz, amplitude));
        return path;
    }

    public static double Rms(ReadOnlySpan<float> samples)
    {
        if (samples.Length == 0)
        {
            return 0.0;
        }

        double sum = 0;
        foreach (var s in samples)
        {
            sum += (double)s * s;
        }

        return Math.Sqrt(sum / samples.Length);
    }

    public static float PeakAbs(ReadOnlySpan<float> samples)
    {
        float peak = 0;
        foreach (var s in samples)
        {
            peak = Math.Max(peak, Math.Abs(s));
        }

        return peak;
    }
}

/// <summary>A synthetic WAV file on disk, deleted on dispose. AudioPlayer.PlayAsync takes a file
/// path (it opens the file itself), so player-level tests need a real file even when the decoder
/// used is a fake that ignores its content.</summary>
internal sealed class TempWavFile : IDisposable
{
    public string Path { get; }

    public TempWavFile(int sampleRate, int channels, int bitsPerSample, int frameCount, double freqHz, double amplitude = 0.5)
    {
        Path = TestAudio.WriteTempSineWav(sampleRate, channels, bitsPerSample, frameCount, freqHz, amplitude);
    }

    public void Dispose() => File.Delete(Path);
}
