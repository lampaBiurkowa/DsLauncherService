using DsCore.ApiClient;
using Microsoft.Extensions.Caching.Memory;

namespace DsLauncherService.Services;

public class CacheService(IMemoryCache cache)
{
    const string ACCESS_TOKEN_KEY = "ACCESS_TOKEN";
    const string REFRESH_TOKEN_KEY = "REFRESH_TOKEN";
    const string USER_KEY = "USER";
    readonly IMemoryCache cache = cache;

    public void SetRefreshToken(string token) => cache.Set(REFRESH_TOKEN_KEY, token);
    public void SetAccessToken(string token) => cache.Set(ACCESS_TOKEN_KEY, token);
    public string? GetAccessToken() => Get<string>(ACCESS_TOKEN_KEY);
    public void SetUser(Guid userGuid) => cache.Set(USER_KEY, userGuid);
    public Guid? GetUser() => Get<Guid>(USER_KEY);

    T? Get<T>(string key)
    {
        var result = cache.TryGetValue<T>(key, out var value);
        if (!result) return default;
        
        return value;
    }
}