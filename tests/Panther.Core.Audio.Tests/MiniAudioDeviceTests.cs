using Panther.Core.Audio.Enums;

namespace Panther.Core.Audio.Tests;

/// <summary>
/// Smoke tests against the real <see cref="MiniAudioDevice"/> - the only place in this suite that
/// touches actual audio hardware. Every test starts by checking whether a usable default playback
/// device exists and skips (not fails) if not, via <c>[SkippableFact]</c>/<c>Skip.If</c>, so this
/// stays safe on a CI runner with no sound card while still exercising the real native P/Invoke
/// plumbing (context/device init, the UnmanagedCallersOnly callback trampolines, GCHandle
/// threading) when hardware is present. Calling Start() without subscribing to RequestData plays
/// silence (HandleDataCallback clears the buffer when there's no handler), so these are safe to
/// run unattended even when a device is present.
/// </summary>
public class MiniAudioDeviceTests
{
    [SkippableFact]
    public void GetCapabilities_ReportsAtLeastOneSampleRateAndChannel()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        // Act
        var capabilities = device!.GetCapabilities();

        // Assert
        capabilities.SupportedSampleRates.ShouldNotBeEmpty();
        capabilities.MaxChannels.ShouldBeGreaterThan(0);
        capabilities.MaxBitsPerSample.ShouldBeGreaterThanOrEqualTo(capabilities.MinBitsPerSample);
    }

    [SkippableFact]
    public void NegotiatedFormat_IsNullUntilConfigured()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        // Assert (nothing has been configured yet - there's no distinct Act)
        device!.NegotiatedFormat.ShouldBeNull();
    }

    [SkippableFact]
    public void Configure_SharedMode_NegotiatesAFormatAndReportsIt()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        // Act
        device!.Configure(RequestedFormat(device), exclusiveMode: false);

        // Assert
        device.NegotiatedFormat.ShouldNotBeNull();
        device.NegotiatedFormat!.Value.SampleRate.ShouldBeGreaterThan(0);
        device.NegotiatedFormat.Value.Channels.ShouldBeGreaterThan(0);
    }

    [SkippableFact]
    public void Configure_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        var format = RequestedFormat(device!);
        device!.Configure(format, exclusiveMode: false);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => device.Configure(format, exclusiveMode: false));
    }

    [SkippableFact]
    public void StartStop_CanBeCalledRepeatedly_WithoutThrowing()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);
        device!.Configure(RequestedFormat(device), exclusiveMode: false);

        // Act & Assert
        Should.NotThrow(() =>
        {
            device.Start();
            device.Stop();
            device.Start(); // pause/resume-style reuse, as AudioPlayer does
            device.Stop();
        });
    }

    [SkippableFact]
    public void Start_BeforeConfigure_ThrowsInvalidOperationException()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => device!.Start());
    }

    [SkippableFact]
    public void Stop_BeforeConfigure_ThrowsInvalidOperationException()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => device!.Stop());
    }

    [SkippableFact]
    public async Task Start_ActuallyInvokesRequestDataOnTheRealAudioThread()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);
        device!.Configure(RequestedFormat(device), exclusiveMode: false);

        var callbackCount = 0;
        var framesSeen = 0;
        device.RequestData += (buffer, frameCount) =>
        {
            Interlocked.Increment(ref callbackCount);
            Interlocked.Add(ref framesSeen, frameCount);
            buffer.Span.Clear(); // explicit silence - nothing audible
        };

        // Act
        device.Start();
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (Volatile.Read(ref callbackCount) == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }
        device.Stop();

        // Assert
        callbackCount.ShouldBeGreaterThan(0, "Expected the real audio thread to invoke RequestData at least once within 2 seconds.");
        framesSeen.ShouldBeGreaterThan(0);
    }

    [SkippableFact]
    public void Dispose_BeforeConfigure_DoesNotThrow()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);

        // Act & Assert
        Should.NotThrow(device!.Dispose);
    }

    [SkippableFact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        using var device = TryCreate(out var skipReason);
        Skip.If(device is null, skipReason);
        device!.Dispose();

        // Act & Assert
        Should.NotThrow(device.Dispose);
    }

    [SkippableFact]
    public void MiniAudioDeviceFactory_CreateDevice_ReturnsAUsableDevice()
    {
        // Arrange
        Skip.IfNot(HasUsableDevice(out var skipReason), skipReason);
        var factory = new MiniAudioDeviceFactory();

        // Act
        using var device = factory.CreateDevice();

        // Assert
        device.GetCapabilities().SupportedSampleRates.ShouldNotBeEmpty();
    }

    private static bool HasUsableDevice(out string skipReason)
    {
        var device = TryCreate(out skipReason);
        device?.Dispose();
        return device is not null;
    }

    private static AudioFormat RequestedFormat(MiniAudioDevice device)
    {
        var capabilities = device.GetCapabilities();
        return new AudioFormat(capabilities.SupportedSampleRates[0], Math.Max(1, capabilities.MaxChannels), SampleFormat.Float32, 32);
    }

    private static MiniAudioDevice? TryCreate(out string skipReason)
    {
        try
        {
            var device = new MiniAudioDevice();
            var capabilities = device.GetCapabilities();
            if (capabilities.SupportedSampleRates.Length == 0)
            {
                device.Dispose();
                skipReason = "The default playback device reported no supported sample rates (no usable audio device on this machine).";
                return null;
            }

            skipReason = "";
            return device;
        }
        catch (Exception ex)
        {
            skipReason = $"No usable playback device on this machine: {ex.Message}";
            return null;
        }
    }
}
