using Microsoft.Extensions.Logging;
using Panther.Core;
using Panther.Core.Models;

namespace Panther.Infrastructure;

public class FileTrackProvider(ILogger<FileTrackProvider> logger) : ITrackInfoProvider
{
    public TrackInfo? LoadFrom(string source)
    {
        try
        {
            using var file = TagLib.File.Create(source);
            return new TrackInfo
            {
                Source = source,
                Duration = file.Properties.Duration.TotalSeconds,
                Title = string.IsNullOrWhiteSpace(file.Tag.Title)
                    ? Path.GetFileNameWithoutExtension(source)
                    : file.Tag.Title,
                Album = file.Tag.Album ?? "Unknown Album",
                Artists = file.Tag.Performers.Length > 0
                    ? [.. file.Tag.Performers]
                    : ["Unknown Artist"],
                AlbumArtists = file.Tag.AlbumArtists.Length > 0
                    ? [.. file.Tag.AlbumArtists]
                    : ["Unknown Artist"],
                TrackNumber = file.Tag.Track,
                DiscNumber = file.Tag.Disc,
                Year = file.Tag.Year,
                Genres = file.Tag.Genres.Length > 0 ? [.. file.Tag.Genres] : ["Unknown Genre"],
                AlbumArt = file.Tag.Pictures.Length > 0 ? file.Tag.Pictures[0].Data.Data : null,
                Codec = file.Properties.Codecs.FirstOrDefault()?.Description ?? "Unknown",
                Bitrate = file.Properties.AudioBitrate,
                BitsPerSample = file.Properties.BitsPerSample,
                SampleRate = file.Properties.AudioSampleRate
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load track from source: {Source}", source);
            return null;
        }
    }
}
