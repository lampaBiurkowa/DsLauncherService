using DsLauncherService.Communication;

namespace DsLauncherService.Handlers
{
    internal interface ICommandHandler
    {
        Task<Command> Handle(CommandArgs args, CancellationToken cancellationToken);
    }
}
