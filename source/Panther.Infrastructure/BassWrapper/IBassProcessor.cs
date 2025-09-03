using Panther.Infrastructure.Models;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public interface IBassProcessor
{
    void Init(int device, int freq, BASSInit flags, nint win);
    void Free();
    float GetVolume();
    void SetVolume(float volume);

    IBassChannel StreamCreateFile(string file,
        long offset = 0L, long length = 0L, BASSFlag flags = BASSFlag.BASS_DEFAULT);
}
