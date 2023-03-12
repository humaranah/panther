using Panther.Core.Models;

namespace Panther.Core.Events;

public class TrackChangedEventArgs : EventArgs
{
    public TrackChangedEventArgs(TrackInfo? current, TrackInfo? last)
    {
        Current = current;
        Last = last;
    }

    public TrackInfo? Current { get; }
    public TrackInfo? Last { get; }
}
