using Panther.Core.Enums;

namespace Panther.Core.Models
{
    public interface IReadOnlyPlaylist : IReadOnlyCollection<TrackInfo>
    {
        string Name { get; }
        PlayerSource Source { get; }
    }
}
