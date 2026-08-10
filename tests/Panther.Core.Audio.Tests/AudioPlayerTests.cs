using Panther.Core.Audio.Enums;
using Panther.Core.Audio.Tests.Fakes;

namespace Panther.Core.Audio.Tests;

/// <summary>
/// Exercises AudioPlayer's orchestration logic - state machine, fades, exclusive-mode fallback,
/// event wiring, thread-safety - against a <see cref="FakeAudioDevice"/> instead of real hardware,
/// so it's deterministic and doesn't depend on a working audio device being present. Most tests
/// still decode a real synthetic WAV through the real <see cref="AudioDecoderFactory"/>; a few use
/// <see cref="ConstantAudioDecoder"/>/<see cref="ThrowingAudioDecoder"/> where precise control over
/// decoded sample values or failure timing matters more than exercising the real decode path.
/// </summary>
public class AudioPlayerTests
{
    private const int FadeFrames44100 = 661; // 44100 * AudioPlayer's 0.015s fade duration, floored

    [Fact]
    public async Task PlayAsync_ConfiguresDeviceWithTrackFormat_AndStartsPlaying()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        var settings = new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never };
        using var player = new AudioPlayer(new AudioDecoderFactory(), deviceFactory, settings);
        using var wav = new TempWavFile(22050, 1, 16, 2205, 440);

        // Act
        await player.PlayAsync(wav.Path);

        // Assert
        player.State.ShouldBe(PlaybackState.Playing);
        player.CurrentFilePath.ShouldBe(wav.Path);
        deviceFactory.LastDevice.ShouldNotBeNull();
        deviceFactory.LastDevice!.IsStarted.ShouldBeTrue();
        deviceFactory.LastDevice.RequestedFormat!.Value.SampleRate.ShouldBe(22050);
        deviceFactory.LastDevice.RequestedFormat.Value.Channels.ShouldBe(1);
        deviceFactory.LastDevice.RequestedExclusiveMode.ShouldBe(false);
        player.Duration.ShouldNotBeNull();
        player.Duration!.Value.TotalSeconds.ShouldBeInRange(0.09, 0.11);
    }

    [Fact]
    public async Task PlayAsync_NonexistentFile_ThrowsAndLeavesPlayerStopped()
    {
        // Arrange
        using var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());
        var missingPath = Path.Combine(Path.GetTempPath(), $"panther-does-not-exist-{Guid.NewGuid():N}.wav");

        // Act
        var action = () => player.PlayAsync(missingPath);

        // Assert
        await action.ShouldThrowAsync<FileNotFoundException>();
        player.State.ShouldBe(PlaybackState.Stopped);
    }

    [Fact]
    public async Task PlayAsync_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());

        // Act
        var action = () => player.PlayAsync("");

        // Act & Assert
        await action.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Pump_ImmediatelyAfterPlay_ProducesLinearFadeInEnvelope()
    {
        // Arrange
        const int sampleRate = 44100;
        var format = new AudioFormat(sampleRate, 1, SampleFormat.Float32, 32);
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new ConstantAudioDecoderFactory(format, value: 1f), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wav = new TempWavFile(sampleRate, 1, 16, 100, 440); // content is ignored by the fake decoder
        await player.PlayAsync(wav.Path);

        // Act
        var output = deviceFactory.LastDevice!.Pump(FadeFrames44100);

        // Assert
        output[0].ShouldBeInRange(0f, 0.01f);
        output[FadeFrames44100 / 2].ShouldBeInRange(0.45f, 0.55f);
        output[^1].ShouldBeInRange(0.98f, 1.0f);
    }

    [Fact]
    public async Task Pause_FadesOutSmoothly_WhenTheAudioThreadKeepsPumping()
    {
        // Arrange
        const int sampleRate = 44100;
        var format = new AudioFormat(sampleRate, 1, SampleFormat.Float32, 32);
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new ConstantAudioDecoderFactory(format, value: 1f), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wav = new TempWavFile(sampleRate, 1, 16, 100, 440);
        await player.PlayAsync(wav.Path);
        deviceFactory.LastDevice!.Pump(1000); // past the fade-in, steady full gain

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await PumpWhile(deviceFactory.LastDevice, () => player.Pause());
        sw.Stop();

        // Assert
        player.State.ShouldBe(PlaybackState.Paused);
        deviceFactory.LastDevice.IsStarted.ShouldBeFalse();
        sw.ElapsedMilliseconds.ShouldBeLessThan(150,
            "expected Pause() to complete via the fade-out, not the 200ms safety timeout.");
    }

    [Fact]
    public void Pause_WhenNotPlaying_IsNoOp()
    {
        // Arrange
        using var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());

        // Act
        player.Pause();

        // Assert
        player.State.ShouldBe(PlaybackState.Stopped);
    }

    [Fact]
    public async Task Resume_AfterPause_ResumesPlaybackAndPositionKeepsAdvancing()
    {
        // Arrange
        const int sampleRate = 44100;
        var format = new AudioFormat(sampleRate, 1, SampleFormat.Float32, 32);
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new ConstantAudioDecoderFactory(format, value: 1f), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wav = new TempWavFile(sampleRate, 1, 16, 100, 440);
        await player.PlayAsync(wav.Path);
        deviceFactory.LastDevice!.Pump(1000);
        await PumpWhile(deviceFactory.LastDevice, () => player.Pause());
        var positionAtPause = player.Position;

        // Act
        player.Resume();
        deviceFactory.LastDevice.Pump(1000);

        // Assert
        player.State.ShouldBe(PlaybackState.Playing);
        deviceFactory.LastDevice.IsStarted.ShouldBeTrue();
        player.Position.ShouldBeGreaterThan(positionAtPause);
    }

    [Fact]
    public async Task Stop_ClearsSessionAndStopsDevice()
    {
        // Arrange
        var format = new AudioFormat(44100, 1, SampleFormat.Float32, 32);
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new ConstantAudioDecoderFactory(format), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wav = new TempWavFile(44100, 1, 16, 100, 440);
        await player.PlayAsync(wav.Path);

        // Act
        await PumpWhile(deviceFactory.LastDevice!, () => player.Stop());

        // Assert
        player.State.ShouldBe(PlaybackState.Stopped);
        player.CurrentFilePath.ShouldBeNull();
        deviceFactory.LastDevice!.IsStarted.ShouldBeFalse();
    }

    [Fact]
    public async Task Seek_ClampsToValidRange()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wav = new TempWavFile(44100, 2, 16, 4410, 440); // 0.1s
        await player.PlayAsync(wav.Path);

        // Act (seek before the start)
        player.Seek(TimeSpan.FromSeconds(-5));

        // Assert
        player.Position.ShouldBe(TimeSpan.Zero);

        // Act (seek past the end)
        player.Seek(TimeSpan.FromSeconds(100));

        // Assert
        player.Position.ShouldBe(player.Duration!.Value);
    }

    [Fact]
    public async Task Seek_WithoutLoadedTrack_Throws()
    {
        // Arrange
        using var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());

        // Act
        var action = () => player.Seek(TimeSpan.FromSeconds(1));

        // Act & Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public async Task PlaybackCompleted_FiresOnNaturalEnd_AndClearsState()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        var completed = new TaskCompletionSource();
        player.PlaybackCompleted += (_, _) => completed.TrySetResult();
        using var wav = new TempWavFile(44100, 1, 16, 100, 440); // very short: 100 frames
        await player.PlayAsync(wav.Path);

        // Act
        // The first pump(s) may still return the tail of the (short) track without hitting zero;
        // keep pumping until one genuinely returns nothing, which is what triggers completion.
        for (var i = 0; i < 5; i++)
        {
            deviceFactory.LastDevice!.Pump(500);
        }

        var finished = await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        finished.ShouldBeSameAs(completed.Task);
        player.State.ShouldBe(PlaybackState.Stopped);
        player.CurrentFilePath.ShouldBeNull();
        deviceFactory.LastDevice!.IsStarted.ShouldBeFalse();
    }

    [Fact]
    public async Task PlaybackError_FiresOnDecodeException_AndStopsCleanly()
    {
        // Arrange
        var format = new AudioFormat(44100, 1, SampleFormat.Float32, 32);
        var deviceFactory = new FakeAudioDeviceFactory();
        var decoderFactory = new ThrowingAudioDecoderFactory(format, failAfterReads: 1);
        using var player = new AudioPlayer(
            decoderFactory, deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        var errorSignal = new TaskCompletionSource<Exception>();
        player.PlaybackError += (_, ex) => errorSignal.TrySetResult(ex);
        using var wav = new TempWavFile(44100, 1, 16, 100, 440); // content ignored by the fake decoder
        await player.PlayAsync(wav.Path);

        // Act
        deviceFactory.LastDevice!.Pump(256); // 1st read: succeeds
        deviceFactory.LastDevice.Pump(256);  // 2nd read: throws
        var finished = await Task.WhenAny(errorSignal.Task, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        finished.ShouldBeSameAs(errorSignal.Task);
        var reportedException = await errorSignal.Task;
        reportedException.ShouldBeOfType<InvalidOperationException>();
        player.State.ShouldBe(PlaybackState.Stopped);
        player.CurrentFilePath.ShouldBeNull();
    }

    [Fact]
    public void Volume_ClampsToZeroToOne()
    {
        // Arrange
        using var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());

        // Act (above the maximum)
        player.Volume = 1.5;

        // Assert
        player.Volume.ShouldBe(1.0, 0.001);

        // Act (below the minimum)
        player.Volume = -0.5;

        // Assert
        player.Volume.ShouldBe(0.0, 0.001);
    }

    [Fact]
    public async Task Volume_AppliesLogarithmicTaper_NotLinear()
    {
        // Arrange
        const int sampleRate = 44100;
        var format = new AudioFormat(sampleRate, 1, SampleFormat.Float32, 32);
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new ConstantAudioDecoderFactory(format, value: 1f), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wav = new TempWavFile(sampleRate, 1, 16, 100, 440);
        await player.PlayAsync(wav.Path);
        deviceFactory.LastDevice!.Pump(FadeFrames44100 + 100); // past the fade-in

        // Act
        player.Volume = 0.5;
        var output = deviceFactory.LastDevice.Pump(100);

        // Assert
        // Same -60dB-floor taper documented on AudioPlayer.Volume / MinVolumeDecibels.
        var expectedGain = Math.Pow(10.0, -60.0 * (1.0 - 0.5) / 20.0);
        output.ShouldAllBe(sample => sample >= (float)(expectedGain * 0.98) && sample <= (float)(expectedGain * 1.02));
        expectedGain.ShouldBeLessThan(0.5, "a log-taper 0.5 should be much quieter than a linear 0.5 would be.");
    }

    [Fact]
    public async Task ExclusiveMode_Always_PropagatesFailureInsteadOfFallingBack()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory { FailNextExclusiveConfigure = true };
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Always });
        using var wav = new TempWavFile(44100, 2, 16, 4410, 440);

        // Act
        var action = () => player.PlayAsync(wav.Path);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
        player.State.ShouldBe(PlaybackState.Stopped);
        player.CurrentFilePath.ShouldBeNull();
    }

    [Fact]
    public async Task ExclusiveMode_PreferExclusive_FallsBackToSharedOnFailure()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory { FailNextExclusiveConfigure = true };
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.PreferExclusive });
        using var wav = new TempWavFile(44100, 2, 16, 4410, 440);

        // Act
        await player.PlayAsync(wav.Path);

        // Assert
        player.State.ShouldBe(PlaybackState.Playing);
        deviceFactory.CreatedDevices.ShouldHaveSingleItem(); // the failed device was reconfigured, not recreated
        deviceFactory.LastDevice!.RequestedExclusiveMode.ShouldBe(false);
    }

    [Fact]
    public async Task ExclusiveMode_Never_ReusesTheSameDeviceAcrossTracks()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wavA = new TempWavFile(44100, 2, 16, 4410, 440);
        using var wavB = new TempWavFile(44100, 2, 16, 4410, 220);

        // Act
        await player.PlayAsync(wavA.Path);
        await player.PlayAsync(wavB.Path);

        // Assert
        deviceFactory.CreatedDevices.ShouldHaveSingleItem();
        deviceFactory.LastDevice!.RequestedExclusiveMode.ShouldBe(false);
    }

    [Fact]
    public async Task ExclusiveMode_Always_RecreatesTheDeviceForEachTrack()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Always });
        using var wavA = new TempWavFile(44100, 2, 16, 4410, 440);
        using var wavB = new TempWavFile(48000, 2, 16, 4800, 220);

        // Act
        await player.PlayAsync(wavA.Path);
        await player.PlayAsync(wavB.Path);

        // Assert
        deviceFactory.CreatedDevices.Count.ShouldBe(2);
        deviceFactory.CreatedDevices.ShouldAllBe(d => d.RequestedExclusiveMode == true);
    }

    [Fact]
    public async Task AllowResampling_False_ThrowsOnSharedModeRateMismatch()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory { NegotiatedFormatOverride = f => f with { SampleRate = 48000 } };
        var settings = new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never, AllowResampling = false };
        using var player = new AudioPlayer(new AudioDecoderFactory(), deviceFactory, settings);
        using var wav = new TempWavFile(44100, 2, 16, 4410, 440);

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => player.PlayAsync(wav.Path));

        // Assert
        ex.Message.ShouldContain("44100");
        ex.Message.ShouldContain("48000");
        player.State.ShouldBe(PlaybackState.Stopped);
    }

    [Fact]
    public async Task AllowResampling_True_AllowsSharedModeRateMismatch()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory { NegotiatedFormatOverride = f => f with { SampleRate = 48000 } };
        var settings = new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never, AllowResampling = true };
        using var player = new AudioPlayer(new AudioDecoderFactory(), deviceFactory, settings);
        using var wav = new TempWavFile(44100, 2, 16, 4410, 440);

        // Act
        await player.PlayAsync(wav.Path);

        // Assert
        player.State.ShouldBe(PlaybackState.Playing);
    }

    [Fact]
    public async Task DeviceDisconnected_StopsCleanlyAndRaisesEvent()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        var raised = false;
        player.DeviceDisconnected += (_, _) => raised = true;
        using var wav = new TempWavFile(44100, 2, 16, 4410, 440);
        await player.PlayAsync(wav.Path);

        // Act
        deviceFactory.LastDevice!.RaiseDisconnected();

        // Assert
        raised.ShouldBeTrue();
        player.State.ShouldBe(PlaybackState.Stopped);
        player.CurrentFilePath.ShouldBeNull();
        deviceFactory.LastDevice.IsStarted.ShouldBeFalse();
    }

    [Fact]
    public async Task DeviceDisconnected_FromAStaleReplacedDevice_IsIgnored()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Always });
        using var wavA = new TempWavFile(44100, 2, 16, 4410, 440);
        using var wavB = new TempWavFile(48000, 2, 16, 4800, 220);
        await player.PlayAsync(wavA.Path);
        var staleDevice = deviceFactory.LastDevice!;
        await player.PlayAsync(wavB.Path); // Always mode recreates the device for track B.
        var raised = false;
        player.DeviceDisconnected += (_, _) => raised = true;

        // Act
        staleDevice.RaiseDisconnected(); // a late notification from the old, already-replaced device

        // Assert
        raised.ShouldBeFalse();
        player.State.ShouldBe(PlaybackState.Playing);
        player.CurrentFilePath.ShouldBe(wavB.Path);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());
        player.Dispose();

        // Act & Assert
        Should.NotThrow(player.Dispose);
    }

    [Fact]
    public async Task PlayAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new AudioPlayer(new AudioDecoderFactory(), new FakeAudioDeviceFactory(), new PlaybackSettings());
        player.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => player.PlayAsync("anything.wav"));
    }

    [Fact]
    public async Task PlayAsync_WhileAlreadyPlaying_SwitchesToTheNewTrackCleanly()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wavA = new TempWavFile(44100, 2, 16, 44100, 440); // 1s
        using var wavB = new TempWavFile(44100, 2, 16, 4410, 220);  // 0.1s
        await player.PlayAsync(wavA.Path);

        // Act
        await player.PlayAsync(wavB.Path);

        // Assert
        player.CurrentFilePath.ShouldBe(wavB.Path);
        player.State.ShouldBe(PlaybackState.Playing);
        player.Duration!.Value.TotalSeconds.ShouldBeInRange(0.09, 0.11);
    }

    [Fact]
    public async Task PlayAsync_ConcurrentCalls_AreSerializedWithoutCorruption()
    {
        // Arrange
        var deviceFactory = new FakeAudioDeviceFactory();
        using var player = new AudioPlayer(
            new AudioDecoderFactory(), deviceFactory,
            new PlaybackSettings { ExclusiveMode = ExclusiveModePreference.Never });
        using var wavA = new TempWavFile(44100, 2, 16, 4410, 440);
        using var wavB = new TempWavFile(44100, 2, 16, 4410, 220);

        // Act
        await Task.WhenAll(player.PlayAsync(wavA.Path), player.PlayAsync(wavB.Path));

        // Assert
        player.State.ShouldBe(PlaybackState.Playing);
        (player.CurrentFilePath == wavA.Path || player.CurrentFilePath == wavB.Path).ShouldBeTrue();
        deviceFactory.CreatedDevices.ShouldHaveSingleItem(); // Never mode: the two serialized calls reuse one device
    }

    /// <summary>Runs <paramref name="action"/> while a background task keeps calling
    /// <see cref="FakeAudioDevice.Pump"/> in a loop, simulating a real audio thread so fade
    /// envelopes (and anything waiting on them, like FadeOutAndStopDeviceLocked) can actually
    /// advance and complete.</summary>
    private static async Task PumpWhile(FakeAudioDevice device, Action action)
    {
        using var cts = new CancellationTokenSource();
        var pumpTask = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                device.Pump(256);
                Thread.Sleep(1);
            }
        });

        try
        {
            action();
        }
        finally
        {
            await cts.CancelAsync();
            await pumpTask;
        }
    }
}
