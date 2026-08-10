using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio.Tests;

public class MiniAudioResamplerTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveArguments()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new MiniAudioResampler(0, 44100, 48000));
        Should.Throw<ArgumentOutOfRangeException>(() => new MiniAudioResampler(2, 0, 48000));
        Should.Throw<ArgumentOutOfRangeException>(() => new MiniAudioResampler(2, 44100, 0));
    }

    [Fact]
    public void Process_Upsampling_ProducesExactExpectedFrameCount()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(channels: 2, sourceSampleRate: 44100, targetSampleRate: 96000);
        var input = GenerateSine(44100, 2, 44100, 440, 0.5);
        var expectedOutputFrames = resampler.GetExpectedOutputFrameCount(44100);
        var output = new float[(expectedOutputFrames + 16) * 2];

        // Act
        var (consumed, produced) = resampler.Process(input, output);

        // Assert
        consumed.ShouldBe(44100);
        produced.ShouldBe(expectedOutputFrames);
        (produced / 44100.0).ShouldBeInRange(2.1, 2.3); // ~96000/44100
    }

    [Fact]
    public void Process_Downsampling_ProducesExactExpectedFrameCount()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(2, 96000, 44100);
        var input = GenerateSine(96000, 2, 96000, 440, 0.5);
        var expected = resampler.GetExpectedOutputFrameCount(96000);
        var output = new float[(expected + 16) * 2];

        // Act
        var (consumed, produced) = resampler.Process(input, output);

        // Assert
        consumed.ShouldBe(96000);
        produced.ShouldBe(expected);
        (produced / 96000.0).ShouldBeInRange(0.44, 0.47); // ~44100/96000
    }

    [Fact]
    public void Process_SameRate_IsExactOneToOnePassthrough()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(2, 48000, 48000);
        var input = GenerateSine(48000, 2, 480, 440, 0.5);
        var output = new float[480 * 2];

        // Act
        var (consumed, produced) = resampler.Process(input, output);

        // Assert
        consumed.ShouldBe(480);
        produced.ShouldBe(480);
    }

    [Fact]
    public void Process_PreservesSignalEnergy()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(1, 44100, 48000);
        var input = GenerateSine(44100, 1, 44100, 440, 0.5);
        var expected = resampler.GetExpectedOutputFrameCount(44100);
        var output = new float[expected + 16];

        // Act
        var (_, produced) = resampler.Process(input, output);
        var sourceRms = TestAudio.Rms(input);
        var outputRms = TestAudio.Rms(output.AsSpan(0, produced));

        // Assert
        outputRms.ShouldBeInRange(sourceRms * 0.9, sourceRms * 1.1);
    }

    [Fact]
    public void GetRequiredInputFrameCount_MatchesWhatProcessActuallyConsumes()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(2, 44100, 96000);
        const int desiredOutputFrames = 2000;

        // Act
        var requiredInput = resampler.GetRequiredInputFrameCount(desiredOutputFrames);
        var input = GenerateSine(44100, 2, requiredInput, 440, 0.5);
        var output = new float[desiredOutputFrames * 2];
        var (consumed, produced) = resampler.Process(input, output);

        // Assert
        consumed.ShouldBe(requiredInput);
        produced.ShouldBe(desiredOutputFrames);
    }

    [Fact]
    public void Reset_RestoresTheSameOutputAsAFreshResampler()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(1, 44100, 48000);
        var input = GenerateSine(44100, 1, 1000, 440, 0.5);
        var outputA = new float[2000];
        var (_, producedA) = resampler.Process(input, outputA);

        // Act
        resampler.Reset();
        var outputB = new float[2000];
        var (_, producedB) = resampler.Process(input, outputB);

        // Assert
        producedB.ShouldBe(producedA);
        outputB.AsSpan(0, producedB).ToArray().ShouldBe(outputA.AsSpan(0, producedA).ToArray());
    }

    [Fact]
    public void Process_AfterDispose_Throws()
    {
        // Arrange
        var resampler = new MiniAudioResampler(2, 44100, 48000);
        resampler.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => resampler.Process(new float[10], new float[10]));
    }

    [Fact]
    public void GetRequiredInputFrameCount_AfterDispose_Throws()
    {
        // Arrange
        var resampler = new MiniAudioResampler(2, 44100, 48000);
        resampler.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => resampler.GetRequiredInputFrameCount(100));
    }

    [Fact]
    public void GetExpectedOutputFrameCount_AfterDispose_Throws()
    {
        // Arrange
        var resampler = new MiniAudioResampler(2, 44100, 48000);
        resampler.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => resampler.GetExpectedOutputFrameCount(100));
    }

    [Fact]
    public void Reset_AfterDispose_Throws()
    {
        // Arrange
        var resampler = new MiniAudioResampler(2, 44100, 48000);
        resampler.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(resampler.Reset);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var resampler = new MiniAudioResampler(2, 44100, 48000);
        resampler.Dispose();

        // Act & Assert
        Should.NotThrow(resampler.Dispose);
    }

    [Fact]
    public void Process_InputLengthNotAMultipleOfChannelCount_Throws()
    {
        // Arrange
        using var resampler = new MiniAudioResampler(2, 44100, 48000);

        // Act & Assert
        Should.Throw<ArgumentException>(() => resampler.Process(new float[9], new float[10]));
    }

    [Theory]
    [InlineData(ResampleQuality.None, ResampleQuality.None)]
    [InlineData(ResampleQuality.Linear, ResampleQuality.Linear)]
    [InlineData(ResampleQuality.Sinc16, ResampleQuality.Linear)]
    [InlineData(ResampleQuality.Sinc32, ResampleQuality.Linear)]
    [InlineData(ResampleQuality.Sinc64, ResampleQuality.Linear)]
    public void ResolveEffectiveQuality_OnlyLinearIsBacked_NoneStaysNone(ResampleQuality requested, ResampleQuality expected)
    {
        // Act
        var resolved = MiniAudioResampler.ResolveEffectiveQuality(requested);

        // Assert
        resolved.ShouldBe(expected);
    }

    private static float[] GenerateSine(int sampleRate, int channels, int frameCount, double freqHz, double amplitude)
    {
        var buffer = new float[frameCount * channels];
        for (var frame = 0; frame < frameCount; frame++)
        {
            var value = (float)(Math.Sin(2 * Math.PI * freqHz * frame / sampleRate) * amplitude);
            for (var ch = 0; ch < channels; ch++)
            {
                buffer[(frame * channels) + ch] = value;
            }
        }

        return buffer;
    }
}
