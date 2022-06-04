using Panther.Core.Constants;
using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Extensions;
using Panther.Core.Models;

namespace Panther.Core.Services;

public class PlayerQueue : IPlayerQueue
{
    private List<TrackInfo> _tracks;
    private int _index;

    public PlayerQueue()
    {
        _tracks = new();
        _index = -1;
    }

    #region Properties
    public bool IsLoaded => _tracks.Any() && _index >= 0;

    public bool IsShuffleEnabled { get; set; }

    public RepeatMode RepeatMode { get; set; }

    public TrackInfo? Current
    {
        get => _index >= 0 ? _tracks[_index] : null;
        set => _index = _tracks.FindIndex(x => x.FileName == value?.FileName);
    }

    public int Count => _tracks.Count;

    public event EventHandler<QueueTrackChangedEventArgs>? QueueTrackChanged;
    #endregion

    #region Implemented Methods
    public void Load(IEnumerable<TrackInfo> tracks)
    {
        _tracks = new(tracks);
        _index = 0;
    }

    public void Add(TrackInfo track)
    {
        _tracks.Add(track);
        if (_index < 0)
        {
            _index = 0;
        }
    }

    public void Remove(TrackInfo track)
    {
        if (!IsLoaded)
        {
            throw new InvalidOperationException(ErrorMessages.QueueNotInitialized);
        }

        var found = _tracks.FindIndex(x => x.FileName == track.FileName);
        if (found < 0)
        {
            return;
        }

        if (_index == found)
        {
            Next();
        }

        _tracks.RemoveAt(found);
    }

    public TrackInfo? Next()
    {
        if (!IsLoaded)
        {
            throw new InvalidOperationException(ErrorMessages.QueueNotInitialized);
        }

        var previous = _index >= 0 ? _tracks[_index] : null;
        _index = GetNextPosition();
        var current = _index >= 0 ? _tracks[_index] : null;

        QueueTrackChanged?.Invoke(this, new QueueTrackChangedEventArgs(previous, current));
        return current;
    }

    public TrackInfo? Previous()
    {
        if (!IsLoaded)
        {
            throw new InvalidOperationException(ErrorMessages.QueueNotInitialized);
        }

        var previous = _index >= 0 ? _tracks[_index] : null;
        _index = GetPreviousPosition();
        var current = _index >= 0 ? _tracks[_index] : null;

        QueueTrackChanged?.Invoke(this, new QueueTrackChangedEventArgs(previous, current));
        return current;
    }
    #endregion

    #region Private methods
    private int GetNextPosition()
    {
        if (_index < 0)
        {
            return 0;
        }

        if (RepeatMode == RepeatMode.RepeatCurrent)
        {
            return _index;
        }

        if (++_index < _tracks.Count)
        {
            return _index;
        }

        if (RepeatMode == RepeatMode.NoRepeat)
        {
            return -1;
        }

        if (IsShuffleEnabled)
        {
            Shuffle();
        }

        return 0;
    }

    private int GetPreviousPosition()
    {
        if (_index < 0)
        {
            return 0;
        }

        if (RepeatMode == RepeatMode.RepeatCurrent)
        {
            return _index;
        }

        if (--_index >= 0)
        {
            return _index;
        }

        if (RepeatMode == RepeatMode.NoRepeat)
        {
            return -1;
        }

        if (!IsShuffleEnabled)
        {
            return _tracks.Count - 1;
        }

        Shuffle();
        return 0;
    }

    private void Shuffle() => Load(_tracks.Randomize());
    #endregion
}
