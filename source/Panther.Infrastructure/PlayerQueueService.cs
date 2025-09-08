using Panther.Core;
using Panther.Core.Models;
using Panther.Core.Util;
using System.Collections.ObjectModel;

namespace Panther.Infrastructure;

public class PlayerQueueService(IRandomProvider random) : IPlayerQueueService
{
    private readonly List<TrackInfo> _source = [];
    private readonly List<TrackInfo> _remaining = [];

    private int _currentIndex = -1;

    public ObservableCollection<TrackInfo> History { get; private set; } = [];

    public IReadOnlyCollection<TrackInfo> Source => _source;

    public IReadOnlyCollection<TrackInfo> Remaining => _remaining;

    public TrackInfo? Current => _currentIndex >= 0 && _currentIndex < History.Count ? History[_currentIndex] : null;

    public bool IsEmpty => _source.Count == 0;
    public bool IsRepeat { get; set; }
    public bool IsShuffle { get; set; }

    public TrackInfo? GetNext()
    {
        if (IsEmpty)
            return null;
        if (_remaining.Count == 0)
        {
            if (!IsRepeat)
                return null;
            _remaining.AddRange(_source);
        }
        var nextOffset = IsShuffle ? random.Next(_remaining.Count) : 0;
        var next = _remaining[nextOffset];
        _remaining.RemoveAt(nextOffset);
        History.Add(next);
        _currentIndex++;
        return next;
    }

    public TrackInfo? GetPrevious()
    {
        if (History.Count == 0)
            return null;
        _currentIndex--;
        return _currentIndex >= 0 ? History[_currentIndex] : null;
    }

    public TrackInfo? SetCurrent(TrackInfo item)
    {
        if (IsEmpty || History.Count == 0)
            return null;
        var index = History.IndexOf(item);
        if (index < 0 || index >= History.Count)
            return Current;
        if (index != _currentIndex)
            _currentIndex = index;
        return History[_currentIndex];
    }

    public void AddToSource(TrackInfo item)
    {
        _source.Add(item);
        if (History.Count > 0)
        {
            _remaining.Add(item);
            return;
        }
        History.Add(item);
        _currentIndex = 0;
    }

    public bool AddToSource(IEnumerable<TrackInfo> items)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
            return false;
        _source.AddRange(itemsList);
        _remaining.AddRange(itemsList);
        if (History.Count == 0)
        {
            History.Add(itemsList[0]);
            _remaining.RemoveAt(0);
            _currentIndex = 0;
        }
        return true;
    }

    public bool RemoveFromSource(TrackInfo item)
    {
        if (IsEmpty)
            return false;
        var index = _source.IndexOf(item);
        if (index < 0)
            return false;
        _source.Remove(item);
        _remaining.Remove(item);
        var historyIndex = History.IndexOf(item);
        if (historyIndex >= 0)
        {
            History.RemoveAt(historyIndex);
            if (History.Count == 0)
                _currentIndex = -1;
            else if (historyIndex <= _currentIndex)
                _currentIndex = Math.Clamp(--_currentIndex, 0, History.Count - 1);
        }
        return true;
    }

    public bool ReplaceSource(IEnumerable<TrackInfo> items)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
            return false;
        _source.Clear();
        _source.AddRange(itemsList);
        _remaining.Clear();
        History.Clear();
        History.Add(itemsList[0]);
        _currentIndex = 0;
        return true;
    }

    public void Clear()
    {
        _source.Clear();
        _remaining.Clear();
        History.Clear();
        _currentIndex = -1;
    }
}
