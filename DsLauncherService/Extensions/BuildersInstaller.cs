using DsLauncherService.Args;
using DsLauncherService.Builders;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DsLauncherService.Extensions;

internal static class BuildersInstaller
{
    public static void InstallCommandBuilders(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();

        var commandArgTypes = assembly.GetTypes()
            .Where(type => typeof(ICommandArgs).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        var commandBuilderTypes = assembly.GetTypes()
            .Where(type => type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandBuilder<>)))
            .ToList();

        foreach (var commandArgType in commandArgTypes)
            foreach (var commandBuilderType in commandBuilderTypes)
            {
                var builderInterface = commandBuilderType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandBuilder<>)
                    && i.GetGenericArguments().First() == commandArgType);

                if (builderInterface != null)
                    services.AddSingleton(builderInterface, commandBuilderType);
            }
    }
}