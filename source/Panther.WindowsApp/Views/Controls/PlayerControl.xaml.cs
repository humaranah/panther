using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Panther.WindowsApp.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Panther.WindowsApp.Views.Controls;

public sealed partial class PlayerControl : UserControl
{
    public PlayerControl()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PlayerViewModel>();
        DataContext = ViewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PositionChanged += OnPositionChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PositionChanged -= OnPositionChanged;
    }

    public PlayerViewModel ViewModel { get; private set; }

    private void OnPositionChanged(object? sender, double position)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            SeekBar.Value = position;
            ViewModel.PositionInSeconds = (int)position;
        });
    }
}
