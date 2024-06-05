using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("add-library")]
internal class AddLibraryCommandHandler(Repository<Library> libraryRepo, GetLibrariesCommandBuilder builder) : ICommandHandler<GetLibrariesCommandArgs>
{    
    public async Task<Response<GetLibrariesCommandArgs>> Handle(CommandArgs args, CancellationToken ct)
    {
        var libraryPath = args.Get<string>("library").Trim();
        var libraryName = args.Get<string>("name").Trim();
        if (Directory.Exists(libraryPath) && !IsDirectoryEmpty(libraryPath))
            throw new();
        
        await libraryRepo.InsertAsync(new() { Path = libraryPath, Name = libraryName }, ct);
        await libraryRepo.CommitAsync(ct);

        return await builder.Build(ct);
    }

    static bool IsDirectoryEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
}
