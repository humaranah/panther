using Panther.Core.Audio.Abstractions;

namespace Panther.Core.Audio.Tests.Fakes;

/// <summary>
/// Stands in for a real playback device so AudioPlayer's orchestration logic (state machine,
/// fades, exclusive-mode fallback, event wiring) can be tested without real audio hardware. Tests
/// drive the "audio thread" explicitly via <see cref="Pump"/>, which synchronously invokes
/// whatever AudioPlayer subscribed to <see cref="RequestData"/> - exactly like the real callback,
/// just called from the test thread instead of miniaudio's.
/// </summary>
internal sealed class FakeAudioDevice(FakeAudioDeviceFactory factory) : IAudioDevice
{
    public bool IsDisposed { get; private set; }
    public bool IsStarted { get; private set; }
    public AudioFormat? RequestedFormat { get; private set; }
    public bool? RequestedExclusiveMode { get; private set; }
    public AudioFormat? NegotiatedFormat { get; private set; }

    public event Action<Memory<float>, int>? RequestData;
    public event EventHandler? Disconnected;

    public DeviceCapabilities GetCapabilities() => factory.Capabilities;

    public void Configure(AudioFormat format, bool exclusiveMode)
    {
        if (NegotiatedFormat is not null)
        {
            throw new InvalidOperationException("The device has already been configured.");
        }

        RequestedFormat = format;
        RequestedExclusiveMode = exclusiveMode;

        if (exclusiveMode && factory.FailNextExclusiveConfigure)
        {
            factory.FailNextExclusiveConfigure = false;
            throw new InvalidOperationException("Simulated exclusive-mode failure.");
        }

        NegotiatedFormat = factory.NegotiatedFormatOverride?.Invoke(format) ?? format;
    }

    public void Start() => IsStarted = true;

    public void Stop() => IsStarted = false;

    public void Dispose()
    {
        IsStarted = false;
        IsDisposed = true;
    }

    /// <summary>
    /// Test hook simulating one real-time audio callback: allocates a buffer sized for
    /// <paramref name="frameCount"/> frames at the negotiated channel count, invokes whatever
    /// AudioPlayer subscribed to <see cref="RequestData"/>, and returns the filled buffer for
    /// inspection (e.g. checking a fade ramp's shape).
    /// </summary>
    public float[] Pump(int frameCount)
    {
        var channels = (NegotiatedFormat ?? RequestedFormat)?.Channels
            ?? throw new InvalidOperationException("The device has not been configured yet.");

        var buffer = new float[frameCount * channels];
        RequestData?.Invoke(buffer, frameCount);
        return buffer;
    }

    /// <summary>Test hook simulating an unexpected stop (e.g. the device was unplugged).</summary>
    public void RaiseDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);
}
