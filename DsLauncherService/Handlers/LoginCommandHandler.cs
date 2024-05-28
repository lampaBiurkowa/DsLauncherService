using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("login")]
internal class LoginCommandHandler(RefreshTokenService refreshTokenService) : ICommandHandler
{
    readonly RefreshTokenService refreshTokenService = refreshTokenService;

    public Task Handle(CommandArgs args, CancellationToken cancellationToken)
    {
        refreshTokenService.SetCredentials(new()
        {
            UserGuid = args.Get<Guid>("userId"),
            PasswordHash = args.Get<string>("passwordBase64"),
            Token = args.Get<string>("token")
        });

        return Task.CompletedTask;
    }
}

