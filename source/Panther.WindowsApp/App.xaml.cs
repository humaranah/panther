using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Panther.WindowsApp.Services;
using Panther.WindowsApp.ViewModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Panther.WindowsApp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
        InitializeComponent();
    }

    public static Window MainWindow => ((App)Current)._window!;

    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton(new ResourceLoader())
            .AddSingleton<INavigationService, NavigationService>()
            .AddTransient<NavigationViewModel>()
            .AddTransient<PlayerViewModel>();
    }
}
