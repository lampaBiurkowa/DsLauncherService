using DsLauncherService.Builders;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DsLauncherService.Extensions;

internal static class BuildersInstaller
{
    public static void InstallCommandBuilders(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var commandBuilders = assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(ICommandBuilder)));
        foreach (var commandBuilder in commandBuilders)
            services.AddSingleton(commandBuilder);
    }
}