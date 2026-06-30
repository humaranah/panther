using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Panther.App.Platform.Windows;

namespace Panther.App.Views;

public partial class MainWindow : Window
{
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
        if (DataContext is not Panther.Core.ViewModels.MainWindowViewModel vm) return;

        if (sender is FANavigationView nav && ReferenceEquals(e.SelectedItemContainer, nav.SettingsItem))
        {
            vm.NavigateTo("settings");
            return;
        }

        var tag = (e.SelectedItemContainer as FANavigationViewItem)?.Tag as string;
        if (tag is not null) vm.NavigateTo(tag);
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e) =>
        BeginMoveDrag(e);

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
