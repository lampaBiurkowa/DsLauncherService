﻿using DibBase.Infrastructure;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("add-library")]
internal class AddLibraryCommandHandler(
    Repository<Library> libraryRepo,
    GetLibrariesCommandBuilder builder,
    GetDeveloperLibrariesCommandBuilder developerBuilder
) : ICommandHandler
{    
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var libraryPath = args.Get<string>("library").Trim();
        var libraryName = args.Get<string>("name").Trim();
        var isDeveloper = args.Get<bool>("isDeveloper");
        if (Directory.Exists(libraryPath) && !IsDirectoryEmpty(libraryPath))
            throw new("Direcotry not empty");
        
        await libraryRepo.InsertAsync(new() { Path = libraryPath, Name = libraryName, IsDeveloper = isDeveloper }, ct);
        await libraryRepo.CommitAsync(ct);

        return isDeveloper ? await developerBuilder.Build(ct) : await builder.Build(ct);
    }

    static bool IsDirectoryEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
}
