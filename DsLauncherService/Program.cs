﻿using DsLauncherService.Communication;
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
                ctx.Configuration.AddDsLauncher(services);
                services.AddSingleton<ServerProvider>();
                services.AddSingleton<CommandDispatcher>();
                services.AddDbContext<DbContext, DsLauncherServiceContext>();
                services.AddHostedSingleton<CommandService>();
                services.AddSingleton<InstallationService>();
                services.AddSingleton<CacheService>();
                services.AddMemoryCache();
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
}
