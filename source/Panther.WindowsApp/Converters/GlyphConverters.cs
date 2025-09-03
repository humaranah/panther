using Panther.Core.Enums;

namespace Panther.WindowsApp.Converters;

public static class GlyphConverters
{
    public static string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uEC9F" : "\uEDC9";

    public static string GetShuffleGlyph(bool isShuffleActive) => isShuffleActive ? "\uE13D" : "\uE13F";

    public static string GetRepeatGlyph(RepeatMode repeatMode) => repeatMode switch
    {
        RepeatMode.All => "\uE127",
        RepeatMode.Single => "\uE125",
        _ => "\uE129"
    };

    public static string GetVolumeGlyph(bool isMuted, int volumePercent)
    {
        if (isMuted) return "\uF029";
        if (volumePercent == 0) return "\uF027";
        if (volumePercent <= 30) return "\uF01B";
        if (volumePercent <= 70) return "\uF01D";
        return "\uF01F";
    }
}
