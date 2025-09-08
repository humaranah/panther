using Panther.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Panther.Core;

public interface IPlayerQueueService : INotifyPropertyChanged
{
    ObservableCollection<TrackInfo> History { get; }
    IReadOnlyCollection<TrackInfo> Source { get; }
    IReadOnlyCollection<TrackInfo> Remaining { get; }
    TrackInfo? Current { get; }
    bool IsEmpty { get; }
    bool IsRepeat { get; set; }
    bool IsShuffle { get; set; }
    TrackInfo? GetNext();
    TrackInfo? GetPrevious();
    TrackInfo? SetCurrent(TrackInfo item);
    void AddToSource(TrackInfo item);
    bool AddToSource(IEnumerable<TrackInfo> items);
    bool RemoveFromSource(TrackInfo item);
    bool ReplaceSource(IEnumerable<TrackInfo> items);
    void Clear();
}
