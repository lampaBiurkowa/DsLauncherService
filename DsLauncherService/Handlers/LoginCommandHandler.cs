using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("login")]
internal class LoginCommandHandler(RefreshTokenService refreshTokenService) : ICommandHandler
{
    readonly RefreshTokenService refreshTokenService = refreshTokenService;
    
    public Task<Command> Handle(CommandArgs args, CancellationToken cancellationToken)
    {
        var token = args.Get<string>("token");
        var userGuid = args.Get<Guid>("userId");
        refreshTokenService.SetCredentials(new()
        {
            UserGuid = userGuid,
            PasswordHash = args.Get<string>("passwordBase64"),
            Token = token
        });

        return Task.FromResult(new Command("credentials")
        {
            Args =
            {
                { "token", token },
                { "userGuid", userGuid }
            }
        });
    }
}

