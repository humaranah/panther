using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using Panther.Core.Services;
using System.Windows;

namespace Panther.WPF;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services) =>
        services
            .AddSingleton<WaveOutEvent>()
            .AddSingleton<IMusicPlayer, MusicPlayer>()
            .AddSingleton<MainWindow>();

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetService<MainWindow>();
        mainWindow?.Show();
    }
}
