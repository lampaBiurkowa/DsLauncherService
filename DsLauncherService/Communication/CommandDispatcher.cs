using DsLauncherService.Handlers;
using DsLauncherService.Extensions;

namespace DsLauncherService.Communication
{
    internal class CommandDispatcher
    {
        private readonly Dictionary<string, ICommandHandler> _commandHandlers = new();

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
                _commandHandlers.Add(commandAttribute.CommandName, commandHandler);
            }
        }

        public async Task HandleCommand(Command command, CancellationToken cancellationToken)
        {
            try
            {
                await _commandHandlers[command.Name].Handle(command.Args, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Could not find a handler for the {command.Name} command");
            }
        }
    }
}
