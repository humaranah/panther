using Panther.Core.Models;

namespace Panther.Core;

public interface ITrackInfoProvider
{
    TrackInfo? LoadFrom(string source);
}
