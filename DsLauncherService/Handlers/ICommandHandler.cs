using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

internal interface ICommandHandler
{
    Task<Response> Handle(CommandArgs args, CancellationToken cancellationToken);
}