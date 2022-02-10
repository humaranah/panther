using ManagedBass;
using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Interfaces;

namespace Panther.Core.Services
{
    public sealed class MusicPlayer : IMusicPlayer
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
        public PlayerStatus Status { get; private set; } = PlayerStatus.Null;
        public long TrackLength { get => Bass.ChannelGetLength(_stream); }
        public long TrackPosition { get => Bass.ChannelGetPosition(_stream); }
        public float Volume { get; set; }

        public event EventHandler<PlayerStatusChangedEventArgs>? StatusChanged;

        public void Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ApplicationException($"Could not find \"{Path.GetFullPath(fileName)}\"!");
            }

            FileName = fileName;
            _stream = Bass.CreateStream(FileName);
            ChangeStatus(PlayerStatus.Stopped);
        }

        public void Pause()
        {
            Bass.ChannelPause(_stream);
            ChangeStatus(PlayerStatus.Paused);
        }

        public void Play()
        {
            Bass.ChannelPlay(_stream);
            ChangeStatus(PlayerStatus.Playing);
        }

        public void Stop()
        {
            Bass.ChannelStop(_stream);
            ChangeStatus(PlayerStatus.Stopped);
        }

        private void ChangeStatus(PlayerStatus targetStatus)
        {
            var data = new PlayerStatusChangedEventArgs(Status, targetStatus);
            Status = targetStatus;
            StatusChanged?.Invoke(this, data);
        }
    }
}
