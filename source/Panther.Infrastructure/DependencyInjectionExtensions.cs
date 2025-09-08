using Microsoft.Extensions.DependencyInjection;
using Panther.Core;
using Panther.Core.Util;
using Panther.Infrastructure.BassWrapper;

namespace Panther.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPantherComponents(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRandomProvider, SharedRandomProvider>()
            .AddTransient<IPlaybackTimer, PlaybackTimer>()
            .AddSingleton<IBassNetService, BassNetService>()
            .AddSingleton<IBassProcessor, BassProcessor>()
            .AddSingleton<ITrackInfoProvider, FileTrackProvider>()
            .AddSingleton<IPlayerQueueService, PlayerQueueService>()
            .AddSingleton<IMusicPlayer, FileMusicPlayer>();
    }
}
