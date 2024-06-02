using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("add-library")]
internal class AddLibraryCommandHandler(Repository<Library> libraryRepo) : ICommandHandler
{    
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var libraryPath = args.Get<string>("library").Trim();
        if (Directory.Exists(libraryPath) && !IsDirectoryEmpty(libraryPath))
            return Command.Empty;
        
        await libraryRepo.InsertAsync(new() { Path = libraryPath }, ct);
        await libraryRepo.CommitAsync(ct);

        return new Command("get-libraries")
        {
            Args =
            {
                {"libraries", (await libraryRepo.GetAll(ct: ct)).Select(x => x.Path) }
            }
        };
    }

    static bool IsDirectoryEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
}
