using Microsoft.Extensions.Logging;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using StackExchange.Redis;

namespace SourceBase.Infrastructure.Implementations;

public class RedisCacheService(AppSettings appSettings, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly Lazy<IDatabase?> _db = new(() =>
    {
        if (string.IsNullOrWhiteSpace(appSettings.ConnectionStrings.RedisConnection)) return null;
        try
        {
            var options = ConfigurationOptions.Parse(appSettings.ConnectionStrings.RedisConnection);
            options.AbortOnConnectFail = false;
            var mux = ConnectionMultiplexer.Connect(options);
            return mux.GetDatabase();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize Redis connection");
            return null;
        }
    });

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _db.Value;
            if (db is null) return default;
            var value = await db.StringGetAsync(key);
            if (!value.HasValue || value.IsNullOrEmpty || string.IsNullOrWhiteSpace(value)) return default;
            return value.ToString().Deserialize<T>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GetAsync failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            var db = _db.Value;
            if (db is null) return;
            var json = value?.Serialize();
            await db.StringSetAsync(key, json, expiry);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SetAsync failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _db.Value;
            if (db is null) return;
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis RemoveAsync failed for key {Key}", key);
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var db = _db.Value;
            if (db is null) return false;
            await db.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis ping failed");
            return false;
        }
    }
}
