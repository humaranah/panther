using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Panther.WindowsApp.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Panther.WindowsApp;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const double DefaultDpi = 96.0;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        SetMinimumWindowSize(320, 380);
    }

    private void SetMinimumWindowSize(int width, int height)
    {
        var dpi = DisplayDiagnostics.GetDpi(this);
        var scale = dpi / DefaultDpi;

        OverlappedPresenter presenter = OverlappedPresenter.Create();
        presenter.PreferredMinimumWidth = (int)(width * scale);
        presenter.PreferredMinimumHeight = (int)(height * scale);
        AppWindow.SetPresenter(presenter);
    }
}
