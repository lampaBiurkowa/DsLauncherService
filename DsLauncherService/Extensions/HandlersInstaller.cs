using DsLauncherService.Args;
using DsLauncherService.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DsLauncherService.Extensions;

internal static class HandlersInstaller
{
    public static void InstallCommandHandlers(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();

        var commandArgTypes = assembly.GetTypes()
            .Where(type => typeof(ICommandArgs).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        var commandHandlerTypes = assembly.GetTypes()
            .Where(type => type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
            .ToList();

        foreach (var commandArgType in commandArgTypes)
            foreach (var commandHandlerType in commandHandlerTypes)
            {
                var handlerInterface = commandHandlerType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                    && i.GetGenericArguments().First() == commandArgType);

                if (handlerInterface != null)
                    services.AddSingleton(handlerInterface, commandHandlerType);
            }
    }
}