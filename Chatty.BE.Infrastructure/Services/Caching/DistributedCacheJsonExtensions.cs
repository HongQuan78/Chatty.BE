using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Chatty.BE.Infrastructure.Services.Caching;

public static class DistributedCacheJsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T?> GetAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken ct = default
    )
    {
        var payload = await cache.GetStringAsync(key, ct);
        return string.IsNullOrWhiteSpace(payload) ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    public static Task SetAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var payload = JsonSerializer.Serialize(value, JsonOptions);
        return cache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct
        );
    }
}
