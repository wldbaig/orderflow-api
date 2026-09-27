using MediatR;
using OrderFlow.Application.Common;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Reference.Dtos;

namespace OrderFlow.Application.Reference.Queries.GetProductCatalog;

public sealed record GetProductCatalogQuery : IRequest<IReadOnlyList<ProductDto>>;

/// <summary>
/// Read-heavy reference query, served from <see cref="ICacheService"/>. The catalog changes
/// rarely, so it is cached for a few minutes and invalidated explicitly when a product is
/// written (see the create-product handler).
/// </summary>
public sealed class GetProductCatalogQueryHandler : IRequestHandler<GetProductCatalogQuery, IReadOnlyList<ProductDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public GetProductCatalogQueryHandler(IProductRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public Task<IReadOnlyList<ProductDto>> Handle(GetProductCatalogQuery request, CancellationToken ct) =>
        _cache.GetOrCreateAsync(
            CacheKeys.ProductCatalog,
            innerCt => _repository.GetActiveCatalogAsync(innerCt),
            CacheTtl,
            ct);
}
