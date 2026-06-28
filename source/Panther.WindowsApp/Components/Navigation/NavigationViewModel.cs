using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panther.WindowsApp.Components.Navigation;
using Panther.WindowsApp.Services;
using Panther.WindowsApp.Views.Pages;
using System.Collections.ObjectModel;

namespace Panther.WindowsApp.ViewModels;

public partial class NavigationViewModel(INavigationService navigationService) : ObservableObject
{
    public ObservableCollection<NavigationItemViewModel> MenuItems { get; } =
    [
        NavigationItemViewModel.CreateItem("NowPlaying", "\uEDC9"),
        NavigationItemViewModel.CreateSeparator(),
        NavigationItemViewModel.CreateItem("Library", "\uEA8E")
            .WithSubItems(
                NavigationItemViewModel.CreateItem("Artists", "\uE935"),
                NavigationItemViewModel.CreateItem("Albums", "\uE935"),
                NavigationItemViewModel.CreateItem("Songs", "\uE935")),
        NavigationItemViewModel.CreateSeparator(),
        NavigationItemViewModel.CreateItem("Playlists", "\uEBE8"),
        NavigationItemViewModel.CreateSeparator(),
        NavigationItemViewModel.CreateItem("Debug", "\uE2CA")
    ];

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
