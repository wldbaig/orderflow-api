using Microsoft.Extensions.Caching.Memory;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Caching;

/// <summary>
/// <see cref="IMemoryCache"/>-backed cache. A per-key lock prevents a cache stampede
/// (many concurrent misses all hitting the database at once).
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        await Gate.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var value = await factory(ct);
            _cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            return value;
        }
        finally
        {
            Gate.Release();
        }
    }

    public void Remove(string key) => _cache.Remove(key);
}
