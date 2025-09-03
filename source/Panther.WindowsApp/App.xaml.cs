using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Panther.Infrastructure;
using Panther.Infrastructure.BassWrapper;
using Panther.WindowsApp.Services;
using Panther.WindowsApp.ViewModels;
using Serilog;
using Serilog.Events;
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Month,
                restrictedToMinimumLevel: LogEventLevel.Warning)
            .WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Debug)
            .CreateLogger();

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
            InitializeServices();
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
            throw;
        }
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
            .AddPantherComponents()
            .AddSingleton(new ResourceLoader())
            .AddSingleton<INavigationService, NavigationService>()
            .AddTransient<IFilePickerService, FilePickerService>()
            .AddTransient<NavigationViewModel>()
            .AddTransient<PlayerViewModel>()
            .AddLogging(builder => builder.ClearProviders().AddSerilog(Log.Logger));
    }

    private static void InitializeServices()
    {
        Services.GetRequiredService<IBassNetService>().Register();
    }
}
