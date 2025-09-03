using Microsoft.Extensions.DependencyInjection;
using Panther.Core;
using Panther.Infrastructure.BassWrapper;

namespace Panther.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPantherComponents(this IServiceCollection services)
    {
        return services
            .AddSingleton<IBassNetService, BassNetService>()
            .AddSingleton<IBassProcessor, BassProcessor>()
            .AddSingleton<IMusicPlayer, LocalMusicPlayer>();
    }
}
