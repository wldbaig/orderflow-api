using Microsoft.Extensions.Caching.Memory;
using OrderFlow.Infrastructure.Caching;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Caching;

public sealed class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly MemoryCacheService _service;
    private int _factoryCalls;

    public MemoryCacheServiceTests()
    {
        _service = new MemoryCacheService(_memoryCache);
    }

    public void Dispose() => _memoryCache.Dispose();

    private Task<string> Factory(CancellationToken ct)
    {
        _factoryCalls++;
        return Task.FromResult($"value-{_factoryCalls}");
    }

    [Fact]
    public async Task GetOrCreate_RepeatedReads_RunFactoryOnce()
    {
        var first = await _service.GetOrCreateAsync("key", Factory, TimeSpan.FromMinutes(1), CancellationToken.None);
        var second = await _service.GetOrCreateAsync("key", Factory, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Equal("value-1", first);
        Assert.Equal("value-1", second);
        Assert.Equal(1, _factoryCalls);
    }

    [Fact]
    public async Task Remove_ForcesFactoryToRunAgain()
    {
        await _service.GetOrCreateAsync("key", Factory, TimeSpan.FromMinutes(1), CancellationToken.None);

        _service.Remove("key");
        var refreshed = await _service.GetOrCreateAsync("key", Factory, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Equal("value-2", refreshed);
    }

    [Fact]
    public async Task GetOrCreate_DifferentKeys_AreCachedIndependently()
    {
        var a = await _service.GetOrCreateAsync("a", Factory, TimeSpan.FromMinutes(1), CancellationToken.None);
        var b = await _service.GetOrCreateAsync("b", Factory, TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.NotEqual(a, b);
        Assert.Equal(2, _factoryCalls);
    }

    [Fact]
    public async Task GetOrCreate_ConcurrentMisses_RunFactoryOnce()
    {
        var reads = Enumerable.Range(0, 20)
            .Select(_ => _service.GetOrCreateAsync("shared", Factory, TimeSpan.FromMinutes(1), CancellationToken.None))
            .ToArray();

        await Task.WhenAll(reads);

        Assert.Equal(1, _factoryCalls);
    }
}
