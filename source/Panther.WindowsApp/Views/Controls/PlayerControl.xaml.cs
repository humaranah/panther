using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Panther.WindowsApp.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Panther.WindowsApp.Views.Controls;

public sealed partial class PlayerControl : UserControl
{
    public PlayerViewModel ViewModel;

    public PlayerControl()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PlayerViewModel>();
        DataContext = ViewModel;
    }
}
