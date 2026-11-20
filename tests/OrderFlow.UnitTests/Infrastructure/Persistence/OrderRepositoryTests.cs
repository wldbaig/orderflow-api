using OrderFlow.Infrastructure.Persistence.Repositories;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Persistence;

public sealed class OrderRepositoryTests
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    [Fact]
    public async Task AddAsync_ThenSave_PersistsAggregateWithLines()
    {
        var order = OrderFactory.CreateWithLines(("SKU-1", "Widget", 1, 1m), ("SKU-2", "Bolt", 2, 2m));
        using (var writeContext = DbContextFactory.Create(_databaseName))
        {
            var repository = new OrderRepository(writeContext);
            await repository.AddAsync(order, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        using var readContext = DbContextFactory.Create(_databaseName);
        var loaded = await new OrderRepository(readContext).GetByIdAsync(order.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Lines.Count);
        Assert.Equal(5m, loaded.TotalAmount);
    }

    [Fact]
    public async Task AddAsync_WithoutSave_DoesNotPersist()
    {
        var order = OrderFactory.Create();
        using (var writeContext = DbContextFactory.Create(_databaseName))
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        using var readContext = DbContextFactory.Create(_databaseName);

        Assert.Null(await new OrderRepository(readContext).GetByIdAsync(order.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        using var context = DbContextFactory.Create(_databaseName);

        Assert.Null(await new OrderRepository(context).GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
