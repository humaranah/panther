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
}
