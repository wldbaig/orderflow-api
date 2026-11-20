using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Reference.Dtos;
using OrderFlow.Application.Reference.Queries.GetProductCatalog;
using OrderFlow.Infrastructure.Caching;
using Xunit;

namespace OrderFlow.UnitTests.Application.Reference;

public sealed class GetProductCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_CalledTwice_HitsRepositoryOnceThroughCache()
    {
        var repository = Substitute.For<IProductRepository>();
        IReadOnlyList<ProductDto> catalog = new[] { new ProductDto { Sku = "SKU-1", Name = "Widget" } };
        repository.GetActiveCatalogAsync(Arg.Any<CancellationToken>()).Returns(catalog);
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetProductCatalogQueryHandler(repository, new MemoryCacheService(memoryCache));

        var first = await handler.Handle(new GetProductCatalogQuery(), CancellationToken.None);
        var second = await handler.Handle(new GetProductCatalogQuery(), CancellationToken.None);

        Assert.Same(catalog, first);
        Assert.Same(catalog, second);
        _ = repository.Received(1).GetActiveCatalogAsync(Arg.Any<CancellationToken>());
    }
}
