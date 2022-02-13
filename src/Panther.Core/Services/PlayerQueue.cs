using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Interfaces;
using Panther.Core.Models;
using Panther.Core.Extensions;

namespace Panther.Core.Services
{
    public class PlayerQueue : IPlayerQueue
    {
        private LinkedList<TrackInfo> _tracks;
        private LinkedListNode<TrackInfo>? _current;

        public PlayerQueue()
        {
            _tracks = new LinkedList<TrackInfo>();
            _current = null;
        }

        public bool IsShuffleEnabled { get; set; }
        public RepeatMode RepeatMode { get; set; }
        public TrackInfo? Current
        {
            get => _current?.Value;
            set
            {
                if (value != null)
                {
                    _current = _tracks.Find(value);
                }
            }
        }

        public event EventHandler<QueueTrackChangedEventArgs>? QueueTrackChanged;

        public void Load(IEnumerable<TrackInfo> tracks)
        {
            _tracks = new LinkedList<TrackInfo>(tracks);
            _current = _tracks.First;
        }

        public void Add(TrackInfo track)
        {
            if (!_tracks.Any())
            {
                throw new InvalidOperationException(ErrorConstants.QueueNotInitialized);
            }

            _tracks.AddLast(track);
        }

        public void Remove(TrackInfo track)
        {
            if (!_tracks.Any())
            {
                throw new InvalidOperationException(ErrorConstants.QueueNotInitialized);
            }

            var found = _tracks.Find(track);
            if (found == null)
            {
                return;
            }

            if (_current == found)
            {
                Next();
            }

            _tracks.Remove(found);
        }

        public TrackInfo? Next()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(ErrorConstants.QueueNotInitialized);
            }

            _current = GetNext();
            QueueTrackChanged?.Invoke(this, new QueueTrackChangedEventArgs(_current?.Previous?.Value, _current?.Value));
            return _current?.Value;
        }

        public TrackInfo? Previous()
        {
            if (_current == null)
            {
                throw new InvalidOperationException(ErrorConstants.QueueNotInitialized);
            }

            _current = GetPrevious();
            QueueTrackChanged?.Invoke(this, new QueueTrackChangedEventArgs(_current?.Previous?.Value, _current?.Value));
            return _current?.Value;
        }

        #region Private methods
        private LinkedListNode<TrackInfo>? GetNext()
        {
            if (RepeatMode == RepeatMode.RepeatCurrent)
            {
                return _current;
            }

            if (_current!.Next != null)
            {
                return _current.Next;
            }

            if (RepeatMode == RepeatMode.NoRepeat)
            {
                return null;
            }

            if (IsShuffleEnabled)
            {
                Shuffle();
            }

            return _tracks.First;
        }

        private LinkedListNode<TrackInfo>? GetPrevious()
        {
            if (RepeatMode == RepeatMode.RepeatCurrent)
            {
                return _current;
            }

            if (_current!.Previous != null)
            {
                return _current.Previous;
            }

            if (RepeatMode == RepeatMode.NoRepeat)
            {
                return null;
            }

            if (!IsShuffleEnabled && _tracks.Last != null)
            {
                return _tracks.Last;
            }

            Shuffle();

            return _tracks.First;
        }

        private void Shuffle() => Load(_tracks.Randomize());
        #endregion
    }
}
