using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Panther.WindowsApp.Services;

public interface IFilePickerService
{
    Task<StorageFile> PickSingleFileAsync();
    Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync();
}
