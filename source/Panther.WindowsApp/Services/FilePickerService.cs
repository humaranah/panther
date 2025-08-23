using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Panther.WindowsApp.Services;

public class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<StorageFile>> PickFilesAsync()
    {
        var picker = new FileOpenPicker()
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            FileTypeFilter = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" }
        };
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        return await picker.PickMultipleFilesAsync();
    }
}
