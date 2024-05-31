using DsCore.ApiClient;
using Microsoft.Extensions.Caching.Memory;

namespace DsLauncherService.Services;

public class CacheService(IMemoryCache cache, DsCoreClientFactory clientFactory)
{
    const string TOKEN_KEY = "token";
    readonly IMemoryCache cache = cache;
    readonly DsCoreClientFactory clientFactory = clientFactory;

    public void SetToken(string token) => cache.Set(TOKEN_KEY, token);
    
    public string? GetToken()
    {
        var value = cache.TryGetValue<string>(TOKEN_KEY, out var token);
        if (!value) return null;
        
        return token;
    }
}