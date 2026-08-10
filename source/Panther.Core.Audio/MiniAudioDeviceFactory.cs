using Panther.Core.Audio.Abstractions;

namespace Panther.Core.Audio;

public sealed class MiniAudioDeviceFactory : IAudioDeviceFactory
{
    public IAudioDevice CreateDevice() => new MiniAudioDevice();
}
