namespace Panther.Core.Models;

public class TrackChange(
    string? previousTrack,
    string? currentTrack)
{
    public string? PreviousTrack { get; } = previousTrack;
    public string? CurrentTrack { get; } = currentTrack;
}
