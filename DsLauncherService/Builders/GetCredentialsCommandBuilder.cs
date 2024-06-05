using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Builders;

class GetCredentialsCommandBuilder(CacheService cache) : ICommandBuilder<GetCredentialsCommandArgs>
{
    public string Name => "get-credentials";

    public Task<Response<GetCredentialsCommandArgs>> Build(CancellationToken ct) =>
        Task.FromResult(new Response<GetCredentialsCommandArgs>(Name, new()
        {
            Token = cache.GetToken() ?? throw new(),
            UserGuid = cache.GetUser() ?? throw new()
        }));
}