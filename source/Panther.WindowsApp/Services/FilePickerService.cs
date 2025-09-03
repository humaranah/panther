using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Panther.WindowsApp.Services;

public class FilePickerService : IFilePickerService
{
    private readonly List<string> _lossyTypes =
        [".mp3", ".aac", ".mp4", ".ogg", ".m4a", ".m4b", ".wma"];

    private readonly List<string> _losslessTypes =
        [".wav", ".flac", ".alac"];

    public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync()
    {
        var picker = InitializeFilePicker();
        return await picker.PickMultipleFilesAsync();
    }

    public async Task<StorageFile> PickSingleFileAsync()
    {
        var picker = InitializeFilePicker();
        return await picker.PickSingleFileAsync();
    }

    private FileOpenPicker InitializeFilePicker()
    {
        var picker = new FileOpenPicker()
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        _lossyTypes.ForEach(picker.FileTypeFilter.Add);
        _losslessTypes.ForEach(picker.FileTypeFilter.Add);
        picker.FileTypeFilter.Add("*");
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);
        return picker;
    }
}
