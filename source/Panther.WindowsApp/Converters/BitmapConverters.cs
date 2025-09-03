using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Panther.WindowsApp.Converters;

public static class BitmapConverters
{
    public static async Task<ImageSource?> ConvertBytesToImageSource(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        using var stream = new InMemoryRandomAccessStream();
        using var writer = new DataWriter(stream);
        writer.WriteBytes(bytes);
        await writer.StoreAsync();
        await writer.FlushAsync();
        stream.Seek(0);

        var image = new BitmapImage();
        await image.SetSourceAsync(stream);
        return image;
    }
}
