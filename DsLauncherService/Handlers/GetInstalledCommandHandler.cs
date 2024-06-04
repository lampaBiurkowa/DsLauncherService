using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("get-installed")]
internal class GetInstalledCommandHandler(Repository<Installed> repo) : ICommandHandler
{
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        return new Command("get-installed")
        {
            Args = 
            {
                { "get-installed", (await repo.GetAll(ct: ct)).Select(x => $"{x.ProductGuid},{x.PackageGuid}") }
            }
        };
    }
}
