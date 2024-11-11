using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetDeveloperLibrariesCommandBuilder(Repository<Library> repo) : ICommandBuilder
{
    public string Name => "get-developer-libraries";

    public async Task<Response> Build(CancellationToken ct)
    {
        var libraries = await repo.GetAll(restrict: x => x.IsDeveloper, ct: ct);
        var result = libraries.Select(x => new LibraryEntry() { Path = x.Path, Name = x.Name }) ?? [];
        return new Response(Name, new GetLibrariesCommandArgs() { Libraries = result });
    }
}