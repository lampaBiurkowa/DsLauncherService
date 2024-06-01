using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("remove-library")]
internal class RemoveLibraryCommandHandler(Repository<Library> libraryRepo, Repository<Installed> installedRepo) : ICommandHandler
{    
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var libraryPath = args.Get<string>("library").Trim();
        var library = (await libraryRepo.GetAll(restrict: x => x.Path == libraryPath, ct: ct)).FirstOrDefault();
        if (library == null) return Command.Empty;

        var isUsed = (await installedRepo.GetAll(restrict: x => x.LibraryId == library.Id, ct: ct)).Count != 0;
        if (isUsed) return Command.Empty;

        await libraryRepo.DeleteAsync(library.Id, ct);
        await libraryRepo.CommitAsync(ct);

        return Command.Empty;
    }
}
