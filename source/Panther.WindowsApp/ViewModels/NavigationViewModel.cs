using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panther.WindowsApp.Models;
using Panther.WindowsApp.Views.Pages;

namespace Panther.WindowsApp.ViewModels;

public partial class NavigationViewModel(INavigationService navigationService) : ObservableObject
{
    [RelayCommand]
    private void OnNavigate(string? pageName)
    {
        var pageType = pageName switch
        {
            "NowPlaying" => typeof(NowPlayingPage),
            "Artists" => typeof(ArtistsPage),
            "Albums" => typeof(AlbumsPage),
            "Songs" => typeof(SongsPage),
            "Playlists" => typeof(PlaylistsPage),
            "Settings" => typeof(SettingsPage),
            _ => null
        };
        if (pageType != null)
        {
            navigationService.NavigateTo(pageType);
        }
    }
}
