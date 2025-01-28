using System.Net;
using DsCore.ApiClient;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Services;
using Microsoft.Extensions.Options;

namespace DsLauncherService.Handlers;

[Command("login")]
internal class LoginCommandHandler(
    IOptions<DsCoreOptions> options,
    CacheService cache,
    GameActivityService gameActivityService,
    GetCredentialsCommandBuilder builder) : ICommandHandler
{
    static HttpClient? clientWithCookies;
    static readonly CookieContainer cookieContainer = new();

    static readonly HttpClientHandler handler = new()
    {
        CookieContainer = cookieContainer,
        UseCookies = true,
    };

    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        clientWithCookies ??= new HttpClient(handler) { BaseAddress = new Uri(options.Value.Url) };
        var userGuid = args.Get<Guid>("userId");
        var passwordBase64 = args.Get<string>("passwordBase64");
        await (await clientWithCookies.GetAsync($"Configuration/bucket-name", ct)).Content.ReadAsStringAsync(ct);
        await clientWithCookies.PostAsync($"Auth/login/{userGuid}?passwordBase64={passwordBase64}", null, ct);
        var token = await (await clientWithCookies.PostAsync($"Auth/access-token", null, ct)).Content.ReadAsStringAsync(ct);
        if (userGuid != cache.GetUser()) //only on new login - ignore token regeneratin
            _ = gameActivityService.SendLocalActivities(userGuid, ct);

        cache.SetAccessToken(token);
        cache.SetUser(userGuid);

        return await builder.Build(ct);
    }
}

