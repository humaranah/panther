using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio.Tests.Decoders;

/// <summary>
/// Exercises the real miniaudio decode path through the public <see cref="AudioDecoderFactory"/>
/// API against synthetic WAV data. WavDecoder/FlacDecoder/Mp3Decoder all share the same
/// MiniAudioDecoderBase implementation and differ only in which ma_encoding_format they pass to
/// miniaudio, so these tests cover that shared logic; FLAC/MP3's own bitstream parsing is
/// miniaudio's (dr_flac/dr_mp3), not code this project wrote, and there's no practical way to
/// synthesize valid FLAC/MP3 test data by hand.
/// </summary>
public class WavDecoderTests
{
    [Fact]
    public void Probe_ReportsInt16_ForEightBitWav()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 2, 8, 4410, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");

        // Act
        var format = decoder.Probe(stream);

        // Assert
        // miniaudio maps u8 PCM to SampleFormat.Int16 (the enum has no U8 case) - see MapFormat's
        // comment in MiniAudioDecoderBase for why.
        format.Format.ShouldBe(SampleFormat.Int16);
        format.BitsPerSample.ShouldBe(8);
    }

    [Theory]
    [InlineData(16, SampleFormat.Int16)]
    [InlineData(24, SampleFormat.Int24)]
    [InlineData(32, SampleFormat.Int32)]
    public void Probe_ReportsCorrectSampleFormatAndBitDepth(int bitsPerSample, SampleFormat expectedFormat)
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, channels: 2, bitsPerSample, frameCount: 4410, freqHz: 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");

        // Act
        var format = decoder.Probe(stream);

        // Assert
        // AudioFormat has only scalar fields, so a whole-object comparison is safe here (see
        // DeviceCapabilitiesTests for a case where that's NOT true, because of an array field).
        format.ShouldBe(new AudioFormat(44100, 2, expectedFormat, bitsPerSample));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Probe_ReportsCorrectChannelCount(int channels)
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, channels, 16, 4410, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");

        // Act
        var format = decoder.Probe(stream);

        // Assert
        format.Channels.ShouldBe(channels);
    }

    [Theory]
    [InlineData(22050)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void Probe_ReportsCorrectSampleRate(int sampleRate)
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(sampleRate, 2, 16, sampleRate / 10, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");

        // Act
        var format = decoder.Probe(stream);

        // Assert
        format.SampleRate.ShouldBe(sampleRate);
    }

    [Fact]
    public void ReadFrames_DecodesFullTrack_WithCorrectFrameCountAndAmplitude()
    {
        // Arrange
        const int sampleRate = 44100;
        const int channels = 2;
        const int frameCount = 44100;
        const double amplitude = 0.5;

        var bytes = TestAudio.BuildSineWaveWav(sampleRate, channels, 16, frameCount, 440, amplitude);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);

        // Act
        var buffer = new float[8192 * channels];
        long totalRead = 0;
        var peak = 0f;
        int framesRead;
        do
        {
            framesRead = decoder.ReadFrames(buffer, 8192);
            peak = Math.Max(peak, TestAudio.PeakAbs(buffer.AsSpan(0, framesRead * channels)));
            totalRead += framesRead;
        } while (framesRead > 0);

        // Assert
        decoder.TotalFrames.ShouldBe(frameCount);
        totalRead.ShouldBe(frameCount);
        peak.ShouldBeInRange((float)(amplitude * 0.99), (float)(amplitude * 1.01));
        decoder.CurrentFrame.ShouldBe(frameCount);
    }

    [Fact]
    public void ReadFrames_PastEndOfStream_ReturnsZeroAndStaysZero()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 2, 16, 100, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);
        var buffer = new float[1000 * 2];

        // Act
        var firstRead = decoder.ReadFrames(buffer, 1000);
        var secondRead = decoder.ReadFrames(buffer, 1000);
        var thirdRead = decoder.ReadFrames(buffer, 1000);

        // Assert
        firstRead.ShouldBe(100);
        secondRead.ShouldBe(0);
        thirdRead.ShouldBe(0);
    }

    [Fact]
    public void Seek_RepositionsDecoder_AndDecodedSampleMatchesDirectComputation()
    {
        // Arrange
        const int sampleRate = 44100;
        const int channels = 1;
        const int frameCount = 44100;
        const double freq = 440;
        const double amplitude = 0.5;
        const long seekFrame = 22050; // 0.5s in

        var bytes = TestAudio.BuildSineWaveWav(sampleRate, channels, 16, frameCount, freq, amplitude);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);

        // Act (seek)
        decoder.Seek(seekFrame);

        // Assert
        decoder.CurrentFrame.ShouldBe(seekFrame);

        // Act (read the frame at the new position)
        var buffer = new float[channels];
        var read = decoder.ReadFrames(buffer, 1);

        // Assert
        read.ShouldBe(1);
        var expected = Math.Sin(2 * Math.PI * freq * (seekFrame / (double)sampleRate)) * amplitude;
        buffer[0].ShouldBeInRange((float)(expected - 0.01), (float)(expected + 0.01));
    }

    [Fact]
    public void Seek_ToStart_AllowsRereadingIdenticalSamples()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 1, 16, 1000, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);
        var first = new float[1000];
        decoder.ReadFrames(first, 1000);

        // Act
        decoder.Seek(0);

        // Assert
        decoder.CurrentFrame.ShouldBe(0);

        // Act
        var second = new float[1000];
        var read = decoder.ReadFrames(second, 1000);

        // Assert
        read.ShouldBe(1000);
        second.ShouldBe(first);
    }

    [Fact]
    public void SupportsSeek_IsTrueForWav()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 1, 16, 100, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);

        // Act
        var supportsSeek = decoder.SupportsSeek;

        // Assert
        supportsSeek.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_WithoutEverOpening_DoesNotThrow()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 1, 16, 100, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        var decoder = factory.CreateFor(stream, "tone.wav");

        // Act & Assert
        Should.NotThrow(decoder.Dispose);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 1, 16, 100, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);
        decoder.Dispose();

        // Act & Assert
        Should.NotThrow(decoder.Dispose);
    }

    [Fact]
    public void Open_CalledTwice_Throws()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 1, 16, 100, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");
        var format = decoder.Probe(stream);
        decoder.Open(stream, format);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => decoder.Open(stream, format));
    }

    [Fact]
    public void ReadFrames_BeforeOpen_Throws()
    {
        // Arrange
        var bytes = TestAudio.BuildSineWaveWav(44100, 1, 16, 100, 440);
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();
        using var decoder = factory.CreateFor(stream, "tone.wav");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => decoder.ReadFrames(new float[10], 10));
    }

    [Fact]
    public void CreateFor_UnrecognizedContent_Throws()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var stream = new MemoryStream(bytes);
        var factory = new AudioDecoderFactory();

        // Act & Assert
        Should.Throw<Exception>(() => factory.CreateFor(stream, "not-audio.wav"));
    }
}
