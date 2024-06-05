using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetInstalledCommandBuilder(Repository<Installed> repo) : ICommandBuilder
{
    public string Name => "get-installed";

    public async Task<Response> Build(CancellationToken ct) =>
        new Response(Name, new GetInstalledCommandArgs
        {
            Installed = (await repo.GetAll(ct: ct)).ToDictionary(x => x.ProductGuid, x => x.PackageGuid)
        });
}