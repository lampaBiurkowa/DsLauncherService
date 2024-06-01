using DsCore.ApiClient;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("login")]
internal class LoginCommandHandler(DsCoreClientFactory clientFactory, CacheService cache) : ICommandHandler
{    
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var userGuid = args.Get<Guid>("userId");
        var passwordBase64 = args.Get<string>("passwordBase64");
        var token = await clientFactory.CreateClient(string.Empty).Auth_LoginAsync(userGuid, passwordBase64, ct);

        cache.SetToken(token);
        cache.SetUser(userGuid);

        return new Command("credentials")
        {
            Args =
            {
                { "token", token },
                { "userGuid", userGuid }
            }
        };
    }
}

