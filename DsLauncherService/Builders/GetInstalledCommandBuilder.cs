using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetInstalledCommandBuilder(Repository<Installed> repo) : ICommandBuilder<GetInstalledCommandArgs>
{
    public string Name => "get-installed";

    public async Task<Response<GetInstalledCommandArgs>> Build(CancellationToken ct) =>
        new Response<GetInstalledCommandArgs>(Name, new()
        {
            Installed = (await repo.GetAll(ct: ct)).ToDictionary(x => x.ProductGuid, x => x.PackageGuid)
        });
}