using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Extensions;
using Panther.Core.Models;

namespace Panther.Core.Services;

public class PlayerQueue : IPlayerQueue
{
    private readonly LinkedList<TrackInfo> _played;
    private LinkedList<TrackInfo> _remaining;
    private IReadOnlyPlaylist _playlist;
    private TrackInfo? _current;

    public PlayerQueue()
    {
        _playlist = new Playlist();
        _played = new();
        _remaining = new();
        _current = null;
    }

    #region Events
    public event EventHandler<TrackChangedEventArgs>? TrackChanged;
    #endregion

    #region Properties
    public bool IsLoaded => _playlist.Any() || _current != null;

    public bool IsShuffleEnabled { get; set; }

    public RepeatMode RepeatMode { get; set; }

    public IReadOnlyCollection<TrackInfo> Played => _played;

    public IReadOnlyCollection<TrackInfo> Remaining => _remaining;

    public TrackInfo? Current => _current;
    #endregion

    #region Implemented Methods
    public void Load(IReadOnlyPlaylist playlist)
    {
        _playlist = playlist;
        InitializeInternalQueue();
    }

    public void Add(TrackInfo track)
    {
        if (_current != null)
        {
            _played.AddLast(_current);
        }
        _current = track;
        TrackChanged?.Invoke(this, new(_current, _played.LastOrDefault()));
    }

    public void AddLast(TrackInfo track) => _remaining.AddLast(track);

    public TrackInfo? Next()
    {
        if (!_playlist.Any())
        {
            return null;
        }
        var last = _current;
        _current = GetNextTrack();
        TrackChanged?.Invoke(this, new(_current, last));
        return _current;
    }

    public TrackInfo? Previous()
    {
        if (!_played.Any())
        {
            return null;
        }
        var last = _current;
        _current = GetPreviousTrack();
        TrackChanged?.Invoke(this, new(_current, last));
        return _current;
    }

    public void Clear()
    {
        _playlist = new Playlist();
        ClearIntenralQueue();
    }
    #endregion

    #region Private methods
    private TrackInfo? GetNextTrack()
    {
        var hasNext = _remaining.Any();
        return (RepeatMode, hasNext, _current) switch
        {
            (RepeatMode.RepeatCurrent, _, null) => null,
            (RepeatMode.RepeatCurrent, _, _) => _current,
            (RepeatMode.NoRepeat, false, _) => null,
            (RepeatMode.Repeat, false, _) => ReloadAndGetFirst(),
            (_, _, _) => _remaining.PopFirst()
        };
    }

    private TrackInfo? GetPreviousTrack()
    {
        var hasPrevious = _played.Any();
        return (RepeatMode, hasPrevious, _current) switch
        {
            (RepeatMode.RepeatCurrent, _, null) => null,
            (RepeatMode.RepeatCurrent, _, _) => _current,
            (RepeatMode.NoRepeat, false, _) => null,
            (RepeatMode.Repeat, false, _) => CalculatePrevious(),
            (_, _, _) => _played.PopLast()
        };
    }

    private TrackInfo? ReloadAndGetFirst()
    {
        InitializeInternalQueue();
        return _current;
    }

    private TrackInfo? CalculatePrevious()
    {
        if (IsShuffleEnabled)
        {
            _remaining.Randomize();
        }
        _remaining.AddFirst(_current!);
        return _remaining.PopLast();
    }

    private void InitializeInternalQueue()
    {
        ClearIntenralQueue();
        if (!_playlist.Any())
        {
            return;
        }
        _current = _playlist.First();
        _remaining = new LinkedList<TrackInfo>(_playlist.Skip(1));
        if (IsShuffleEnabled)
        {
            _remaining.Randomize();
        }
        TrackChanged?.Invoke(this, new(_current, null));
    }

    private void ClearIntenralQueue()
    {
        _played.Clear();
        _remaining.Clear();
        _current = null;
    }
    #endregion
}
