using DibBase.Infrastructure;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("remove-library")]
internal class RemoveLibraryCommandHandler(
    Repository<Library> libraryRepo,
    Repository<Installed> installedRepo,
    GetLibrariesCommandBuilder builder) : ICommandHandler
{    
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var libraryPath = args.Get<string>("library").Trim();
        var library = (await libraryRepo.GetAll(restrict: x => x.Path == libraryPath, ct: ct)).FirstOrDefault() ?? throw new();
        var isUsed = (await installedRepo.GetAll(restrict: x => x.LibraryId == library.Id, ct: ct)).Count != 0;
        if (isUsed) throw new();

        await libraryRepo.DeleteAsync(library.Id, ct);
        await libraryRepo.CommitAsync(ct);

        return await builder.Build(ct);
    }
}
