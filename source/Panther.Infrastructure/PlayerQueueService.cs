using Panther.Core;
using Panther.Core.Models;

namespace Panther.Infrastructure;

public class PlayerQueueService : IPlayerQueueService
{
    private LinkedList<PlayerQueueItem> _items = [];
    private LinkedListNode<PlayerQueueItem>? _current;

    public IReadOnlyCollection<PlayerQueueItem> Items => _items;

    public PlayerQueueItem? Current => _current?.Value;

    public bool IsEmpty => _items.Count == 0;

    public bool IsFirstPosition => _current?.Value == _items.First?.Value;

    public bool IsLastPosition => _current?.Value == _items.Last?.Value;

    public bool Add(PlayerQueueItem item)
    {
        var lastCount = _items.Count;
        var added = _items.AddLast(item);
        if (lastCount == 0)
            _current = added;
        return true;
    }

    public bool AddRange(IEnumerable<PlayerQueueItem> items)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
            return false;
        var lastCount = _items.Count;
        itemsList.ForEach(item => _items.AddLast(item));
        if (lastCount == 0)
            _current = _items.First;
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        _current = null;
    }

    public PlayerQueueItem? SetCurrentTo(PlayerQueueItem item)
    {
        var node = _items.Find(item);
        if (node == null)
            return null;
        _current = node;
        return node.Value;
    }

    public PlayerQueueItem? Next(bool isLoop)
    {
        if (_current == null)
            return null;
        if (_current.Next != null)
        {
            _current = _current.Next;
            return _current.Value;
        }
        if (isLoop && _items.First != null)
        {
            _current = _items.First;
            return _current.Value;
        }
        return null;
    }

    public PlayerQueueItem? Previous(bool isLoop)
    {
        if (_current == null)
            return null;
        if (_current.Previous != null)
        {
            _current = _current.Previous;
            return _current.Value;
        }
        if (isLoop && _items.Last != null)
        {
            _current = _items.Last;
            return _current.Value;
        }
        return null;
    }

    public bool Remove(PlayerQueueItem item)
    {
        var node = _items.Find(item);
        if (node == null)
            return false;
        if (node == _current)
        {
            if (_current.Next != null)
                _current = _current.Next;
            else if (_current.Previous != null)
                _current = _current.Previous;
            else
                _current = null;
        }
        _items.Remove(node);
        return true;
    }

    public void Shuffle()
    {
        if (_items.Count < 2)
            return;
        var currentItem = _current?.Value;
        var array = _items.ToArray();
        Random.Shared.Shuffle(array);
        _items = new LinkedList<PlayerQueueItem>(array);
        if (currentItem != null)
            _current = _items.Find(currentItem);
    }
}
