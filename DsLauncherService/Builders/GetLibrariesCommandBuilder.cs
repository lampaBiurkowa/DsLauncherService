using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetLibrariesCommandBuilder(Repository<Library> repo) : ICommandBuilder
{
    public string Name => "get-libraries";

    public async Task<Response> Build(CancellationToken ct)
    {
        var libraries = (await repo.GetAll(ct: ct)).Select(x => new LibraryEntry()
        {
            Path = x.Path,
            Name = x.Name
        }) ?? [];

        return new Response(Name, new GetLibrariesCommandArgs() { Libraries = libraries });
    }
}