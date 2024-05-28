using DsLauncherService.Communication;

namespace DsLauncherService.Handlers
{
    internal interface ICommandHandler
    {
        Task Handle(CommandArgs args, CancellationToken cancellationToken);
    }
}
