using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("add-library")]
internal class AddLibraryCommandHandler(Repository<Library> libraryRepo, Repository<Installed> installedRepo) : ICommandHandler
{    
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var libraryPath = args.Get<string>("library").Trim();
        if (Directory.Exists(libraryPath) && !IsDirectoryEmpty(libraryPath))
            return Command.Empty;
        
        await libraryRepo.InsertAsync(new() { Path = libraryPath }, ct);
        await libraryRepo.CommitAsync(ct);

        return Command.Empty;
    }

    static bool IsDirectoryEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
}
