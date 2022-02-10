using ManagedBass;
using Panther.Core.Enums;
using Panther.Core.Interfaces;

namespace Panther.Core.Services
{
    public class MusicPlayer : IMusicPlayer
    {
        private int _stream;

        public MusicPlayer()
        {
            if (!Bass.Init())
            {
                throw new ApplicationException("Could not init BASS!");
            }
        }

        public string FileName { get; private set; } = "";
        public PlayerStatus Status { get; private set; }
        public long TrackLength { get => Bass.ChannelGetLength(_stream); }
        public long TrackPosition { get => Bass.ChannelGetPosition(_stream); }
        public float Volume { get; set; }

        public void Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ApplicationException($"Could not find \"{Path.GetFullPath(fileName)}\"!");
            }

            FileName = fileName;
            _stream = Bass.CreateStream(FileName);
        }

        public void Pause()
        {
            Bass.ChannelPause(_stream);
            Status = PlayerStatus.Paused;
        }

        public void Play()
        {
            Bass.ChannelPlay(_stream);
            Status = PlayerStatus.Playing;
        }

        public void Stop()
        {
            Bass.ChannelStop(_stream);
            Status = PlayerStatus.Stopped;
        }
    }
}
