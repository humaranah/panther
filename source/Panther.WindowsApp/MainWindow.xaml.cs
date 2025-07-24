using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Panther.WindowsApp.Models;
using Panther.WindowsApp.ViewModels;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Panther.WindowsApp;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel;

    public MainWindow()
    {
        InitializeComponent();

        var navigationService = App.Services.GetRequiredService<INavigationService>();
        ContentFrame.Content = navigationService.RootFrame;

        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        NavView.DataContext = ViewModel;

        ViewModel.NavigateCommand.Execute("NowPlaying");
        NavView.SelectedItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == "NowPlaying");

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var tag = (args.InvokedItemContainer as NavigationViewItem)?.Tag?.ToString();
        ViewModel.NavigateCommand.Execute(tag);
    }
}
