using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("get-libraries")]
internal class GetLibrariesCommandHandler(Repository<Library> repo) : ICommandHandler
{    
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var paths = (await repo.GetAll(ct: ct)).Select(x => x.Path);

        var cmd = new Command("get-libraries");
        cmd.Args.Add("libraries", paths);
        return cmd;
    }
}
