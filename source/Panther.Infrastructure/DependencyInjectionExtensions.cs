using Microsoft.Extensions.DependencyInjection;
using Panther.Core;

namespace Panther.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPantherComponents(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMusicPlayer, LocalMusicPlayer>();
    }
}
