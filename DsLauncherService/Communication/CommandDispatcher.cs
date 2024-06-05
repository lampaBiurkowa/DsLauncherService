using DsLauncherService.Extensions;
using DsLauncherService.Handlers;

namespace DsLauncherService.Communication;

internal class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> commandHandlers = [];

    public CommandDispatcher() { }

    public CommandDispatcher(IEnumerable<ICommandHandler> commandHandlers)
    {
        foreach (var commandHandler in commandHandlers)
            AddCommandHandler(commandHandler);
    }

    public void AddCommandHandler(ICommandHandler commandHandler)
    {
        if (commandHandler.GetType().TryGetCustomAttribute<CommandAttribute>(out var commandAttribute))
            commandHandlers.Add(commandAttribute.CommandName, commandHandler);
    }

    public async Task<Response> HandleCommand(Command command, CancellationToken cancellationToken) =>
        await commandHandlers[command.Name].Handle(command.Args, cancellationToken);
}
