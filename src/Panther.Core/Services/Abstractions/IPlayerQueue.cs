using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Models;

namespace Panther.Core.Services;

public interface IPlayerQueue
{
    bool IsLoaded { get; }
    bool IsShuffleEnabled { get; set; }
    RepeatMode RepeatMode { get; set; }
    TrackInfo? Current { get; set; }
    int Count { get; }

    event EventHandler<QueueTrackChangedEventArgs> QueueTrackChanged;

    void Load(IEnumerable<TrackInfo> tracks);
    void Add(TrackInfo track);
    void Remove(TrackInfo track);
    TrackInfo? Previous();
    TrackInfo? Next();
}
