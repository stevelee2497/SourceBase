using Microsoft.Extensions.Caching.Memory;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        cache.Set(key, value, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
}
