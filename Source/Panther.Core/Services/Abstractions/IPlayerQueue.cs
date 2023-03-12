using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Models;

namespace Panther.Core.Services;

public interface IPlayerQueue
{
    event EventHandler<TrackChangedEventArgs>? TrackChanged;

    bool IsLoaded { get; }
    bool IsShuffleEnabled { get; set; }
    RepeatMode RepeatMode { get; set; }
    IReadOnlyCollection<TrackInfo> Played { get; }
    IReadOnlyCollection<TrackInfo> Remaining { get; }
    TrackInfo? Current { get; }

    void Load(IReadOnlyPlaylist playlist);
    void Add(TrackInfo track);
    void AddLast(TrackInfo track);
    TrackInfo? Previous();
    TrackInfo? Next();
}
