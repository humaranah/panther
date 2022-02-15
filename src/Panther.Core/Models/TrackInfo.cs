using Panther.Core.Constants;

namespace Panther.Core.Models
{
    public class TrackInfo
    {
        private string _fileName = "";

        internal TrackInfo() { }

        public TrackInfo(string fileName) => FileName = fileName;

        public TrackInfo(TagLib.File file)
        {
            FileName = file.Name;
            Length = file.Length;
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

        public long Length { get; set; }
        public string Title { get; set; } = "";
        public string Album { get; set; } = "";
        public string Composer { get; set; } = "";

    }
}
