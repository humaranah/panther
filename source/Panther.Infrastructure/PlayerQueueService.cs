using Panther.Core;
using Panther.Core.Models;
using Panther.Core.Util;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Panther.Infrastructure;

public class PlayerQueueService(IRandomProvider random) : IPlayerQueueService
{
    private readonly List<TrackInfo> _source = [];
    private readonly List<TrackInfo> _remaining = [];

    private int _currentIndex = -1;
    private bool _isShuffle;
    private bool _isRepeat;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TrackInfo> History { get; private set; } = [];

    public IReadOnlyCollection<TrackInfo> Source => _source;

    public IReadOnlyCollection<TrackInfo> Remaining => _remaining;

    public TrackInfo? Current => _currentIndex >= 0 && _currentIndex < History.Count ? History[_currentIndex] : null;

    public bool IsEmpty => _source.Count == 0;

    public bool IsRepeat
    {
        get => _isRepeat;
        set
        {
            if (_isRepeat != value)
            {
                _isRepeat = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRepeat)));
            }
        }
    }

    public bool IsShuffle
    {
        get => _isShuffle;
        set
        {
            if (_isShuffle != value)
            {
                _isShuffle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsShuffle)));
            }
        }
    }

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
        SetCurrentIndex(_currentIndex + 1);
        return next;
    }

    public TrackInfo? GetPrevious()
    {
        if (History.Count == 0)
            return null;
        SetCurrentIndex(_currentIndex - 1);
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
            SetCurrentIndex(index);
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
        SetCurrentIndex(0);
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
            SetCurrentIndex(0);
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
        RemoveAllFromHistory(item);
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
        SetCurrentIndex(0);
        return true;
    }

    public void Clear()
    {
        _source.Clear();
        _remaining.Clear();
        History.Clear();
        SetCurrentIndex(-1);
    }

    private void SetCurrentIndex(int index)
    {
        _currentIndex = index;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
    }

    private void RemoveAllFromHistory(TrackInfo item)
    {
        var indexToRemove = History.IndexOf(item);
        var removing = true;
        while(removing)
            removing = History.Remove(item);
        if (History.Count == 0)
        {
            SetCurrentIndex(-1);
            return;
        }
        var newIndex = Math.Clamp(_currentIndex, 0, History.Count - 1);
        if (indexToRemove == _currentIndex)
            SetCurrentIndex(newIndex);
        else if (indexToRemove < _currentIndex)
            _currentIndex = newIndex;
    }
}
