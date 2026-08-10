using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Panther.Core.ViewModels;

namespace Panther.App.Windows.Views.Pages;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; private set; } = new();

    public LibraryPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is LibraryViewModel viewModel)
        {
            ViewModel = viewModel;
            Bindings.Update();
        }
    }
}
