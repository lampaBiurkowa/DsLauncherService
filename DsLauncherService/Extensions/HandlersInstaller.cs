using DsLauncherService.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DsLauncherService.Extensions;

internal static class HandlersInstaller
{
    public static void InstallCommandHandlers(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var commandHandlers = assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(ICommandHandler)));
        foreach (var commandHandler in commandHandlers)
            services.AddSingleton(typeof(ICommandHandler), commandHandler);
    }
}