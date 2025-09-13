using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Panther.WindowsApp.Helpers;

public static partial class DisplayDiagnostics
{
    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hWnd);

    public static double GetDpi(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        return GetDpiForWindow(hwnd);
    }
}
