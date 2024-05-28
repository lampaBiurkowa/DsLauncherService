using DsLauncherService.Communication;
using DsLauncherService.Extensions;
using DsLauncherService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DsCore.ApiClient;
using System.Reflection;

namespace DsLauncherService
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
            .UseContentRoot(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
            .ConfigureServices((ctx, services) =>
            {
                ctx.Configuration.AddDsCore(services);
                services.AddSingleton<ServerProvider>();
                services.AddSingleton<CommandDispatcher>();
                services.AddHostedService<CommandService>();
                services.AddHostedSingleton<RefreshTokenService>();
                services.InstallCommandHandlers();
            }).RunConsoleAsync();
        }
    }
}
