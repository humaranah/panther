using Panther.Core.Models;

namespace Panther.Core;

public interface IPlayerQueueService
{
    IReadOnlyCollection<PlayerQueueItem> Items { get; }
    PlayerQueueItem? Current { get; }
    bool IsEmpty { get; }
    bool IsFirstPosition { get; }
    bool IsLastPosition { get; }
    PlayerQueueItem? Next(bool isLoop);
    PlayerQueueItem? Previous(bool isLoop);
    PlayerQueueItem? SetCurrentTo(PlayerQueueItem item);
    bool Add(PlayerQueueItem item);
    bool AddRange(IEnumerable<PlayerQueueItem> items);
    bool Remove(PlayerQueueItem item);
    void Clear();
    void Shuffle();
}
