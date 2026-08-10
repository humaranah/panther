using Panther.Core.Audio.Abstractions;

namespace Panther.Core.Audio.Tests.Fakes;

internal sealed class FakeAudioDeviceFactory : IAudioDeviceFactory
{
    public List<FakeAudioDevice> CreatedDevices { get; } = [];

    public DeviceCapabilities Capabilities { get; set; } = new([44100, 48000], 16, 32, 2, SupportsExclusiveMode: true);

    /// <summary>When true, the next Configure(..., exclusiveMode: true) call on any device this
    /// factory creates throws, then resets to false - mirrors MiniAudioDevice self-cleaning on a
    /// failed Configure so a caller-side fallback retry (same device, exclusiveMode: false) works.</summary>
    public bool FailNextExclusiveConfigure { get; set; }

    /// <summary>Lets a test simulate the backend negotiating a different format than requested
    /// (e.g. shared mode locked to the OS mixer's rate/channel count).</summary>
    public Func<AudioFormat, AudioFormat>? NegotiatedFormatOverride { get; set; }

    public FakeAudioDevice? LastDevice => CreatedDevices.Count > 0 ? CreatedDevices[^1] : null;

    public IAudioDevice CreateDevice()
    {
        var device = new FakeAudioDevice(this);
        CreatedDevices.Add(device);
        return device;
    }
}
