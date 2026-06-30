using CommunityToolkit.Mvvm.ComponentModel;

namespace Panther.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string AppName = "Panther Music Player";

    public string WindowTitle { get; } = OperatingSystem.IsWindows() ? "" : AppName;
    public string ChromeTitle { get; } = OperatingSystem.IsWindows() ? AppName : "";

    [ObservableProperty]
    private ViewModelBase _currentPage = new NowPlayingViewModel();

    public void NavigateTo(string tag) => CurrentPage = tag switch
    {
        "now-playing" => new NowPlayingViewModel(),
        "library"     => new LibraryViewModel(),
        "playlists"   => new PlaylistsViewModel(),
        "settings"    => new SettingsViewModel(),
        _             => CurrentPage
    };
}
