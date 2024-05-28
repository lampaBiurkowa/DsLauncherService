using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DsLauncherService.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddHostedSingleton<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IHostedService
    {
        services.AddSingleton<TImplementation>();
        services.AddHostedService(s => s.GetRequiredService<TImplementation>());
    }
}