﻿using DsLauncherService.Args;
using DsLauncherService.Extensions;
using DsLauncherService.Handlers;

namespace DsLauncherService.Communication;

internal class CommandDispatcher<T> where T : ICommandArgs
{
    private readonly Dictionary<string, ICommandHandler<T>> commandHandlers = [];

    public CommandDispatcher() { }

    public CommandDispatcher(IEnumerable<ICommandHandler<T>> commandHandlers)
    {
        foreach (var commandHandler in commandHandlers)
            AddCommandHandler(commandHandler);
    }

    public void AddCommandHandler(ICommandHandler<T> commandHandler)
    {
        if (commandHandler.GetType().TryGetCustomAttribute<CommandAttribute>(out var commandAttribute))
            commandHandlers.Add(commandAttribute.CommandName, commandHandler);
    }

    public async Task<Response<T>> HandleCommand(Command command, CancellationToken cancellationToken) =>
        await commandHandlers[command.Name].Handle(command.Args, cancellationToken);
}
