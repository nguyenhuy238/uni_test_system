using Microsoft.Extensions.Caching.Memory;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Services;

public sealed class InMemoryTokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryTokenBlacklistService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task RevokeAsync(string jti, DateTimeOffset expiry)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return Task.CompletedTask;
        }

        var now = DateTimeOffset.UtcNow;
        if (expiry <= now)
        {
            return Task.CompletedTask;
        }

        var ttl = expiry - now;
        _memoryCache.Set(BuildCacheKey(jti), true, ttl);
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return Task.FromResult(false);
        }

        var revoked = _memoryCache.TryGetValue(BuildCacheKey(jti), out _);
        return Task.FromResult(revoked);
    }

    private static string BuildCacheKey(string jti) => $"auth:blacklist:{jti}";
}
