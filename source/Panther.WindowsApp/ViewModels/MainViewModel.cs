using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panther.WindowsApp.Models;
using Panther.WindowsApp.Views;
using System.Runtime.Versioning;

namespace Panther.WindowsApp.ViewModels;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class MainViewModel(INavigationService navigationService) : ObservableObject
{
    public IRelayCommand<string?> NavigateCommand => new RelayCommand<string?>(OnNavigate);

    private void OnNavigate(string? pageName)
    {
        var pageType = pageName switch
        {
            "NowPlaying" => typeof(NowPlayingPage),
            _ => null
        };
        if (pageType != null)
        {
            navigationService.NavigateTo(pageType);
        }
    }
}
