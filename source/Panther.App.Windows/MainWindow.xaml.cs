using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Panther.App.Windows.Views.Pages;
using Panther.Core.ViewModels;

namespace Panther.App.Windows;

public sealed partial class MainWindow : Window
{
    private const double CompactPaneWidthThreshold = 600;
    private const string LibraryTagPrefix = "library:";

    private static readonly LibrarySection[] LibrarySections =
    [
        LibrarySection.Songs,
        LibrarySection.Albums,
        LibrarySection.Artists,
        LibrarySection.Genres
    ];

    private bool _isCompactNavigation;

    public MainWindowViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        SizeChanged += (_, e) => UpdateNavigationPaneDisplayMode(e.Size.Width);

        NavigateToPage(ViewModel.CurrentPage);
    }

    private void NavigateToPage(ViewModelBase viewModel)
    {
        var pageType = viewModel switch
        {
            NowPlayingViewModel => typeof(NowPlayingPage),
            LibraryViewModel => typeof(LibraryPage),
            PlaylistsViewModel => typeof(PlaylistsPage),
            SettingsViewModel => typeof(SettingsPage),
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };

        ContentFrame.Navigate(pageType, viewModel);
    }

    private void UpdateNavigationPaneDisplayMode(double width)
    {
        _isCompactNavigation = width < CompactPaneWidthThreshold;

        NavigationView.PaneDisplayMode = _isCompactNavigation
            ? NavigationViewPaneDisplayMode.LeftMinimal
            : NavigationViewPaneDisplayMode.Top;

        NavigationView.IsPaneToggleButtonVisible = _isCompactNavigation;

        LibraryNavItem.MenuItems.Clear();
        if (_isCompactNavigation)
        {
            foreach (var section in LibrarySections)
            {
                LibraryNavItem.MenuItems.Add(new NavigationViewItem
                {
                    Content = section.ToString(),
                    Tag = LibraryTagPrefix + section
                });
            }
        }

        SyncLibraryCompactState();
    }

    private void SyncLibraryCompactState()
    {
        if (ViewModel.CurrentPage is LibraryViewModel library)
            library.IsCompactLayout = _isCompactNavigation;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ViewModel.NavigateTo("settings");
        }
        else if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            if (tag.StartsWith(LibraryTagPrefix, StringComparison.Ordinal))
            {
                ViewModel.NavigateTo("library");
                if (ViewModel.CurrentPage is LibraryViewModel library &&
                    Enum.TryParse<LibrarySection>(tag[LibraryTagPrefix.Length..], out var section))
                {
                    library.SelectedSection = section;
                }
            }
            else
            {
                ViewModel.NavigateTo(tag);
            }
        }
        else
        {
            return;
        }

        NavigateToPage(ViewModel.CurrentPage);
        SyncLibraryCompactState();
    }
}
