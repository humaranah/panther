using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.ApplicationModel.Resources;
using Panther.Core;
using Panther.Infrastructure;
using Panther.Infrastructure.BassWrapper;
using Panther.WindowsApp.Services;
using Panther.WindowsApp.ViewModels;
using Serilog;
using Serilog.Events;
using System;

namespace Panther.WindowsApp;

public static class StartUp
{
    public static void InitializeLogger()
    {
        var loggerConfiguration = new LoggerConfiguration();
#if DEBUG
        loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose();
#else
        loggerConfiguration = loggerConfiguration.MinimumLevel.Information();
#endif
        Log.Logger = loggerConfiguration
            .WriteTo.File("logs/log.txt",
                fileSizeLimitBytes: 10_000_000,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogEventLevel.Warning)
            .WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Debug)
            .CreateLogger();
    }

    public static IServiceProvider InitializeServices()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IBassNetService>().Register();
        serviceProvider.GetRequiredService<IBassProcessor>().Init();
        return serviceProvider;
    }

    private static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        return services
            .AddPantherComponents()
            .AddSingleton(new ResourceLoader())
            .AddSingleton<INavigationService, NavigationService>()
            .AddTransient<IFilePickerService, FilePickerService>()
            .AddSingleton<IPlaybackTimer, PlaybackTimer>()
            .AddTransient<NavigationViewModel>()
            .AddTransient<PlayerViewModel>()
            .AddLogging(builder => builder.ClearProviders().AddSerilog(Log.Logger));
    }
}
