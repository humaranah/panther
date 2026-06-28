using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Panther.App.Platform.Windows;

internal static partial class WindowsChrome
{
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public static void Configure(Window window)
    {
        window.TransparencyLevelHint = [
            WindowTransparencyLevel.Mica,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.None];
        window.ExtendClientAreaToDecorationsHint = true;
        window.ExtendClientAreaTitleBarHeightHint = -1;
    }

    public static void ApplyRoundedCorners(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null || handle == IntPtr.Zero) return;
        int preference = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(handle.Value, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }

    public static void ApplyRoundedCornersDeferred(Window window) =>
        Dispatcher.UIThread.Post(() => ApplyRoundedCorners(window), DispatcherPriority.Background);
}
