﻿using DsCore.ApiClient;
using DsLauncherService.Args;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("login")]
internal class LoginCommandHandler(
    DsCoreClientFactory clientFactory,
    CacheService cache,
    GameActivityService gameActivityService,
    GetCredentialsCommandBuilder builder) : ICommandHandler<GetCredentialsCommandArgs>
{    
    public async Task<Response<GetCredentialsCommandArgs>> Handle(CommandArgs args, CancellationToken ct)
    {
        var userGuid = args.Get<Guid>("userId");
        var passwordBase64 = args.Get<string>("passwordBase64");
        var token = await clientFactory.CreateClient(string.Empty).Auth_LoginAsync(userGuid, passwordBase64, ct);

        cache.SetToken(token);
        cache.SetUser(userGuid);

        _ = gameActivityService.SendLocalActivities(userGuid, ct);
        return await builder.Build(ct);
    }
}

