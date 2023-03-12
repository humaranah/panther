using Panther.Core.Enums;

namespace Panther.Core.Models;

public class Playlist
    : List<TrackInfo>, IReadOnlyPlaylist, IEquatable<Playlist>
{
    public Playlist() : base()
    {
        Name = string.Empty;
    }

    public Playlist(string name) : base()
    {
        Name = name;
    }

    public string Name { get; set; }
    public PlayerSource Source { get; set; }

    public bool Equals(Playlist? other) =>
        Name == other?.Name && Source == other?.Source;

    public override bool Equals(object? obj) =>
        Equals(obj as Playlist);

    public override int GetHashCode() =>
        HashCode.Combine(nameof(Playlist), Name, Source);
}
