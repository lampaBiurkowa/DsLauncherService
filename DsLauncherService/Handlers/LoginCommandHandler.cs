using DsCore.ApiClient;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("login")]
internal class LoginCommandHandler(
    DsCoreClientFactory clientFactory,
    CacheService cache,
    GameActivityService gameActivityService,
    GetCredentialsCommandBuilder builder) : ICommandHandler
{    
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var userGuid = args.Get<Guid>("userId");
        var passwordBase64 = args.Get<string>("passwordBase64");
        var token = await clientFactory.CreateClient(string.Empty).Auth_LoginAsync(userGuid, passwordBase64, ct);

        if (userGuid != cache.GetUser()) //only on new login - ignore token regeneratin
            _ = gameActivityService.SendLocalActivities(userGuid, ct);

        cache.SetToken(token);
        cache.SetUser(userGuid);

        return await builder.Build(ct);
    }
}

