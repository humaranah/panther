using Panther.Core.Models;

namespace Panther.Core;

public interface IMusicLibrary
{
    string Name { get; }
    ICollection<TrackInfo> Tracks { get; }
}
