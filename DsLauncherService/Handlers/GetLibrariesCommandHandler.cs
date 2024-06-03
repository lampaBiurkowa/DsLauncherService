using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("get-libraries")]
internal class GetLibrariesCommandHandler(Repository<Library> repo) : ICommandHandler
{
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        return new Command("get-libraries")
        {
            Args = 
            {
                { "libraries", (await repo.GetAll(ct: ct)).Select(x => $"{x.Path},{x.Name}") }
            }
        };
    }
}
