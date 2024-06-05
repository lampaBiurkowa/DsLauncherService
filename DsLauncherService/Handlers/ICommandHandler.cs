using DsLauncherService.Args;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

internal interface ICommandHandler<T> where T : ICommandArgs
{
    Task<Response<T>> Handle(CommandArgs args, CancellationToken cancellationToken);
}