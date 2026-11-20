using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Repositories;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Persistence;

public sealed class ProductRepositoryTests : IDisposable
{
    private readonly OrderFlowDbContext _db = DbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        var retired = Product.Create("SKU-OLD", "Old widget", "Hardware", 1m);
        retired.Deactivate();
        _db.Products.AddRange(
            Product.Create("SKU-B", "Bolt", "Hardware", 0.5m),
            Product.Create("SKU-A", "Anchor", "Hardware", 2m),
            Product.Create("SKU-C", "Cable", "Electrical", 3m),
            retired);
        _db.SaveChanges();
        _repository = new ProductRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData("SKU-A", true)]
    [InlineData("SKU-OLD", true)]
    [InlineData("SKU-Z", false)]
    public async Task SkuExistsAsync_ChecksAllProducts(string sku, bool expected)
    {
        Assert.Equal(expected, await _repository.SkuExistsAsync(sku, CancellationToken.None));
    }

    [Fact]
    public async Task GetActiveCatalogAsync_ExcludesInactiveAndSortsByCategoryThenName()
    {
        var catalog = await _repository.GetActiveCatalogAsync(CancellationToken.None);

        Assert.Equal(new[] { "SKU-C", "SKU-A", "SKU-B" }, catalog.Select(p => p.Sku));
    }

    [Fact]
    public async Task AddAsync_ThenSave_PersistsProduct()
    {
        await _repository.AddAsync(Product.Create("SKU-NEW", "New", "Misc", 1m), CancellationToken.None);
        await _repository.SaveChangesAsync(CancellationToken.None);

        Assert.True(await _repository.SkuExistsAsync("SKU-NEW", CancellationToken.None));
    }
}
