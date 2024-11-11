using DsLauncherService.Communication;
using DsLauncherService.Extensions;
using DsLauncherService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DsCore.ApiClient;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using DsLauncherService.Storage;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsNdib.ApiClient;
using Microsoft.Extensions.Configuration;

namespace DsLauncherService;

internal class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
        .UseContentRoot(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
        .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
                          .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables();
                })
        .ConfigureServices((ctx, services) =>
        {
            services.AddDsCore(ctx.Configuration);
            services.AddDsLauncher(ctx.Configuration);
            services.AddDsNdib(ctx.Configuration);
            services.AddSingleton<ServerProvider>();
            services.AddSingleton<CommandDispatcher>();
            services.AddDbContext<DbContext, DsLauncherServiceContext>();
            services.AddHostedSingleton<CommandService>();
            services.AddHostedSingleton<GameActivityService>();
            services.AddSingleton<InstallationService>();
            services.AddSingleton<CacheService>();
            services.AddMemoryCache();
            services.InstallCommandBuilders();
            services.InstallCommandHandlers();

            var entityTypes = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                entityTypes.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(DibBase.ModelBase.Entity))).ToList());
                foreach (var e in entityTypes)
                {
                    var repositoryType = typeof(Repository<>).MakeGenericType(e);
                    services.AddScoped(repositoryType);
                }
            }
        }).RunConsoleAsync();
    }
}