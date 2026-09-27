using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Reference.Dtos;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Reference.Commands.CreateProduct;

public sealed record CreateProductCommand(string Sku, string Name, string Category, decimal UnitPrice)
    : IRequest<ProductDto>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Adds a product and then INVALIDATES the cached catalog, so the read side never serves a
/// stale list. This pairs with <c>GetProductCatalogQuery</c> to demonstrate correct
/// cache invalidation on write.
/// </summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(IProductRepository repository, ICacheService cache, ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        if (await _repository.SkuExistsAsync(request.Sku.Trim(), ct))
            throw new DomainException($"A product with SKU '{request.Sku}' already exists.");

        var product = Product.Create(request.Sku, request.Name, request.Category, request.UnitPrice);
        await _repository.AddAsync(product, ct);
        await _repository.SaveChangesAsync(ct);

        // Invalidate the cached catalog so the next read reflects this write.
        _cache.Remove(CacheKeys.ProductCatalog);
        _logger.LogInformation("Created product {Sku} and invalidated catalog cache.", product.Sku);

        return new ProductDto
        {
            Sku = product.Sku,
            Name = product.Name,
            Category = product.Category,
            UnitPrice = product.UnitPrice
        };
    }
}
