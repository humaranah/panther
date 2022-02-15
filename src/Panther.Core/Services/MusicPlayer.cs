using Panther.Bass;
using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Interfaces;

namespace Panther.Core.Services
{
    public sealed class MusicPlayer : IMusicPlayer, IDisposable
    {
        private readonly IBassPlayer _bass;
        private int _stream;

        public MusicPlayer(IBassPlayer bass)
        {
            _bass = bass;
        }

        public string FileName { get; private set; } = "";
        public PlayerStatus Status { get; private set; } = PlayerStatus.Null;
        public long TrackLength { get => _bass.ChannelGetLength(_stream); }
        public long TrackPosition { get => _bass.ChannelGetPosition(_stream); }
        public float Volume { get; set; }

        public event EventHandler<PlayerStatusChangedEventArgs>? StatusChanged;

        public void Dispose() => _bass.Dispose();

        public void Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ApplicationException(ErrorMessages.CouldNotFind(fileName));
            }

            FileName = fileName;
            _stream = _bass.CreateStream(FileName);
            ChangeStatus(PlayerStatus.Stopped);
        }

        public void Pause()
        {
            _bass.ChannelPause(_stream);
            ChangeStatus(PlayerStatus.Paused);
        }

        public void Play()
        {
            _bass.ChannelPlay(_stream);
            ChangeStatus(PlayerStatus.Playing);
        }

        public void Stop()
        {
            _bass.ChannelStop(_stream);
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
