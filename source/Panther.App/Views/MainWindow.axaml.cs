using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Panther.App.Platform.Windows;
using Panther.Core.ViewModels;

namespace Panther.App.Views;

public partial class MainWindow : Window
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

    public MainWindow()
    {
        ConfigureWindowChrome();
        InitializeComponent();
        ConfigureTitleBar();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                if (OperatingSystem.IsWindows() && WindowState == WindowState.Normal)
                    WindowsChrome.ApplyRoundedCornersDeferred(this);
        };

        SizeChanged += (_, e) => UpdateNavigationPaneDisplayMode(e.NewSize.Width);
    }

    private void UpdateNavigationPaneDisplayMode(double width)
    {
        _isCompactNavigation = width < CompactPaneWidthThreshold;

        NavigationView.PaneDisplayMode = _isCompactNavigation
            ? FANavigationViewPaneDisplayMode.LeftMinimal
            : FANavigationViewPaneDisplayMode.Top;

        // En Windows, el toggle vive en la barra de título personalizada (TitleBarGrid);
        // en Linux/Mac esa barra está oculta (ver ConfigureTitleBar), así que ahí se usa
        // el botón propio del NavigationView como respaldo.
        PaneToggleButton.IsVisible = _isCompactNavigation && OperatingSystem.IsWindows();
        NavigationView.IsPaneToggleButtonVisible = _isCompactNavigation && !OperatingSystem.IsWindows();
        if (!_isCompactNavigation) NavigationView.IsPaneOpen = false;

        LibraryNavItem.MenuItems.Clear();
        if (_isCompactNavigation)
        {
            foreach (var section in LibrarySections)
            {
                LibraryNavItem.MenuItems.Add(new FANavigationViewItem
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
        if (DataContext is MainWindowViewModel { CurrentPage: LibraryViewModel library })
            library.IsCompactLayout = _isCompactNavigation;
    }

    private void ConfigureWindowChrome()
    {
        if (OperatingSystem.IsWindows())
        {
            Background = Brushes.Transparent;
            WindowsChrome.Configure(this);
        }
        else if (OperatingSystem.IsMacOS())
        {
            Background = Brushes.Transparent;
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None];
        }
        // Linux: fondo y decoraciones predeterminadas del WM
    }

    private void ConfigureTitleBar()
    {
        if (OperatingSystem.IsWindows()) return;

        TitleBarGrid.IsVisible = false;
        RootGrid.RowDefinitions[0] = new RowDefinition(GridLength.Auto);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (OperatingSystem.IsWindows())
            WindowsChrome.ApplyRoundedCorners(this);
    }

    private void NavigationView_SelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (sender is FANavigationView nav && ReferenceEquals(e.SelectedItemContainer, nav.SettingsItem))
        {
            vm.NavigateTo("settings");
            return;
        }

        var tag = (e.SelectedItemContainer as FANavigationViewItem)?.Tag as string;
        if (tag is null) return;

        if (tag.StartsWith(LibraryTagPrefix, StringComparison.Ordinal))
        {
            vm.NavigateTo("library");
            if (vm.CurrentPage is LibraryViewModel library &&
                Enum.TryParse<LibrarySection>(tag[LibraryTagPrefix.Length..], out var section))
            {
                library.SelectedSection = section;
            }
        }
        else
        {
            vm.NavigateTo(tag);
        }

        SyncLibraryCompactState();
    }

    private void PaneToggleButton_Click(object? sender, RoutedEventArgs e) =>
        NavigationView.IsPaneOpen = !NavigationView.IsPaneOpen;

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        BeginMoveDrag(e);

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
