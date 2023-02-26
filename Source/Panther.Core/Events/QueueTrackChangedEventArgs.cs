using Panther.Core.Models;

namespace Panther.Core.Events;

public class QueueTrackChangedEventArgs
{
    public QueueTrackChangedEventArgs(TrackInfo? last, TrackInfo? current)
    {
        Last = last;
        Current = current;
    }

    public TrackInfo? Current { get; set; }
    public TrackInfo? Last { get; set; }
}
