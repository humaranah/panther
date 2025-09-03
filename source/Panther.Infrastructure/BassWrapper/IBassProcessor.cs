using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public interface IBassProcessor
{
    bool IsInitialized { get; }

    bool Init();
    bool Free();
    float GetVolume();
    bool SetVolume(float volume);

    IBassChannel StreamCreateFile(string file, BASSFlag flags = BASSFlag.BASS_DEFAULT);
}
