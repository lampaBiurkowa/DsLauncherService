using DsLauncherService.Communication;
using DsLauncherService.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DsLauncherService
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args).ConfigureServices(services =>
            {
                services.AddSingleton<ServerProvider>();
                services.AddSingleton<CommandDispatcher>();
                services.AddHostedService<CommandService>();
                services.InstallCommandHandlers();
            }).RunConsoleAsync();
        }
    }
}
