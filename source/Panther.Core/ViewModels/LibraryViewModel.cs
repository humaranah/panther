using CommunityToolkit.Mvvm.ComponentModel;

namespace Panther.Core.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyMessage))]
    private LibrarySection _selectedSection = LibrarySection.Songs;

    [ObservableProperty]
    private bool _isCompactLayout;

    public string EmptyMessage => SelectedSection switch
    {
        LibrarySection.Songs => "No songs in library",
        LibrarySection.Albums => "No albums in library",
        LibrarySection.Artists => "No artists in library",
        LibrarySection.Genres => "No genres in library",
        _ => string.Empty
    };
}
