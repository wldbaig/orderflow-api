using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderFlow.Application.Common;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Reference.Commands.CreateProduct;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Entities;
using Xunit;

namespace OrderFlow.UnitTests.Application.Reference;

public sealed class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_repository, _cache, NullLogger<CreateProductCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenSkuExists_ThrowsDomainExceptionWithoutSaving()
    {
        _repository.SkuExistsAsync("SKU-1", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(new CreateProductCommand(" SKU-1 ", "Widget", "Hardware", 1m), CancellationToken.None));

        _ = _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        _cache.DidNotReceive().Remove(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenNew_SavesThenInvalidatesCatalogCache()
    {
        _repository.SkuExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = await _handler.Handle(new CreateProductCommand(" SKU-1 ", " Widget ", " Hardware ", 4.5m), CancellationToken.None);

        Assert.Equal("SKU-1", dto.Sku);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal("Hardware", dto.Category);
        Assert.Equal(4.5m, dto.UnitPrice);
        Received.InOrder(() =>
        {
            _repository.AddAsync(Arg.Is<Product>(p => p.Sku == "SKU-1"), Arg.Any<CancellationToken>());
            _repository.SaveChangesAsync(Arg.Any<CancellationToken>());
            _cache.Remove(CacheKeys.ProductCatalog);
        });
    }
}
