using Panther.Core.Models;

namespace Panther.Core.Events;

public class TrackDeletedEventArgs : EventArgs
{
    public TrackDeletedEventArgs(TrackInfo deletedTrack)
    {
        DeletedTrack = deletedTrack;
    }

    public TrackInfo DeletedTrack { get; }
}
