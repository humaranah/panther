using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        DataContext = ViewModel;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.PositionInSeconds) && !ViewModel.IsSeeking)
        {
            SeekBar.Value = ViewModel.PositionInSeconds;
        }
    }

    public PlayerViewModel ViewModel { get; private set; }

    private void SeekBar_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        if (ViewModel.IsSeeking) return;
        ViewModel.IsSeeking = true;
    }

    private void SeekBar_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!ViewModel.IsSeeking) return;
        ViewModel.IsSeeking = false;
        ViewModel.SeekToCommand.Execute(SeekBar.Value);
    }
}
