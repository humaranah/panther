namespace Panther.Core.Models;

public class TrackInfo
{
    public string Source { get; set; } = null!;
    public double Duration { get; set; }
    public string Title { get; set; } = string.Empty;
    public string[] Artists { get; set; } = [];
    public string Album { get; set; } = string.Empty;
    public string[] AlbumArtists { get; set; } = [];
    public uint TrackNumber { get; set; }
    public uint DiscNumber { get; set; }
    public uint Year { get; set; }
    public string[] Genres { get; set; } = [];
    public byte[]? AlbumArt { get; set; }
    public string Codec { get; set; } = string.Empty;
    public int Bitrate { get; set; }
    public int BitsPerSample { get; set; }
    public int SampleRate { get; set; }
}
