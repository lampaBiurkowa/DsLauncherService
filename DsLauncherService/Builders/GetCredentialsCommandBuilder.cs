using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Builders;

class GetCredentialsCommandBuilder(CacheService cache) : ICommandBuilder
{
    public string Name => "get-credentials";

    public Task<Response> Build(CancellationToken ct) =>
        Task.FromResult(new Response(Name, new GetCredentialsCommandArgs
        {
            Token = cache.GetToken() ?? throw new(),
            UserGuid = cache.GetUser() ?? throw new()
        }));
}