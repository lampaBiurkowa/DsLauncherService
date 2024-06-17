using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetRecentsCommandBuilder(Repository<Recents> repo, CacheService cache) : ICommandBuilder
{
    public string Name => "get-recent";

    public async Task<Response> Build(CancellationToken ct) =>
        new Response(Name, new GetRecentsCommandArgs
        {
            Recents = (await repo.GetAll(restrict: x => x.UserGuid == cache.GetUser(), ct: ct)).FirstOrDefault()?.ProductGuids ?? []
        });
}