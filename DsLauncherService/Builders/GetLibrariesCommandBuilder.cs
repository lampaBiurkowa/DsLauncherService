using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetLibrariesCommandBuilder(Repository<Library> repo) : ICommandBuilder<GetLibrariesCommandArgs>
{
    public string Name => "get-libraries";

    public async Task<Response<GetLibrariesCommandArgs>> Build(CancellationToken ct)
    {
        var libraries = (await repo.GetAll(ct: ct)).Select(x => new LibraryEntry()
        {
            Path = x.Path,
            Name = x.Name
        }) ?? [];

        return new Response<GetLibrariesCommandArgs>(Name, new() { Libraries = libraries });
    }
}