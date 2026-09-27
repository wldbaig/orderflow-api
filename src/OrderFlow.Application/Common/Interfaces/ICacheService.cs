namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// Thin abstraction over an in-memory cache, so caching stays testable and out of the
/// query handlers' way. Used by read-heavy reference data with explicit invalidation on write.
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct);

    void Remove(string key);
}
