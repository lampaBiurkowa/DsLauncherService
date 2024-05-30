using DsLauncherService.Extensions;
using DsLauncherService.Handlers;

namespace DsLauncherService.Communication;

internal class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> commandHandlers = new();

    public CommandDispatcher() { }

    public CommandDispatcher(IEnumerable<ICommandHandler> commandHandlers)
    {
        foreach (var commandHandler in commandHandlers)
        {
            AddCommandHandler(commandHandler);
        }
    }

    public void AddCommandHandler(ICommandHandler commandHandler)
    {
        if (commandHandler.GetType().TryGetCustomAttribute<CommandAttribute>(out var commandAttribute))
        {
            commandHandlers.Add(commandAttribute.CommandName, commandHandler);
        }
    }

    public async Task<Command> HandleCommand(Command command, CancellationToken cancellationToken)
    {
        return await commandHandlers[command.Name].Handle(command.Args, cancellationToken);
    }
}
