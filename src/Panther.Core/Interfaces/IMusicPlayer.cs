using Panther.Core.Enums;

namespace Panther.Core.Interfaces
{
    public interface IMusicPlayer
    {
        string FileName { get; }
        PlayerStatus Status { get; }
        long TrackLength { get; }
        long TrackPosition { get; }
        float Volume { get; set; }

        void Load(string fileName);
        void Play();
        void Stop();
        void Pause();
    }
}
