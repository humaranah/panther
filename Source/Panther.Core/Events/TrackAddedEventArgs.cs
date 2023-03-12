using Panther.Core.Models;

namespace Panther.Core.Events;

public class TrackAddedEventArgs : EventArgs
{
    public TrackAddedEventArgs(TrackInfo newTrack)
    {
        NewTrack = newTrack;
    }

    public TrackInfo NewTrack { get; }
}
