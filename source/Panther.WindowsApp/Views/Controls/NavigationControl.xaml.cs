using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Panther.WindowsApp.Services;
using Panther.WindowsApp.ViewModels;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Panther.WindowsApp.Views.Controls;

public sealed partial class NavigationControl : UserControl
{
    public NavigationControl()
    {
        InitializeComponent();

        var navigationService = App.Services.GetRequiredService<INavigationService>();
        ContentFrame.Content = navigationService.RootFrame;

        ViewModel = App.Services.GetRequiredService<NavigationViewModel>();
        NavView.DataContext = ViewModel;

        ViewModel.NavigateCommand.Execute("NowPlaying");
        NavView.SelectedItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == "NowPlaying");

        SetNavigationSettingsLabel();
    }

    public NavigationViewModel ViewModel { get; set; }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var navigationItem = args.InvokedItemContainer as NavigationViewItem;
        var tag = navigationItem?.Tag?.ToString();
        ViewModel.NavigateCommand.Execute(tag);
    }

    private void SetNavigationSettingsLabel()
    {
        var settingsItem = NavView.SettingsItem as NavigationViewItem;
        if (settingsItem != null)
        {
            settingsItem.Content = App.Services.GetRequiredService<ResourceLoader>().GetString("NavSettingsLabel");
        }
    }
}
