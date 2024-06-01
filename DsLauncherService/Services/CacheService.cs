using DsCore.ApiClient;
using Microsoft.Extensions.Caching.Memory;

namespace DsLauncherService.Services;

public class CacheService(IMemoryCache cache, DsCoreClientFactory clientFactory)
{
    const string TOKEN_KEY = "token";
    const string USER_KEY = "user";
    readonly IMemoryCache cache = cache;
    readonly DsCoreClientFactory clientFactory = clientFactory;

    public void SetToken(string token) => cache.Set(TOKEN_KEY, token);
    public string? GetToken() => Get<string>(TOKEN_KEY);
    public void SetUser(Guid userGuid) => cache.Set(USER_KEY, userGuid);
    public Guid? GetUser() => Get<Guid>(USER_KEY);

    T? Get<T>(string key)
    {
        var result = cache.TryGetValue<T>(key, out var value);
        if (!result) return default;
        
        return value;
    }
}