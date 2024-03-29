﻿using Panther.Core.Constants;

namespace Panther.Core.Models;
public record TrackInfo
{
    private string _fileName = string.Empty;

    internal TrackInfo() { }

    public TrackInfo(string fileName)
    {
        FileName = fileName;
    }

    public TrackInfo(TagLib.File file)
    {
        FileName = file.Name;
        Duration = file.Properties.Duration;
        Title = file.Tag.Title;
        Album = file.Tag.Album;
        Composer = file.Tag.JoinedComposers;
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(ErrorMessages.ValueNotNullOrEmpty, nameof(FileName));
            }
            _fileName = value;
        }
    }

    public TimeSpan Duration { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string Composer { get; set; } = string.Empty;

}
