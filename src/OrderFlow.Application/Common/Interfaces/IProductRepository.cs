using OrderFlow.Application.Reference.Dtos;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken ct);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Projected, no-tracking read of the active catalog (the value that gets cached).</summary>
    Task<IReadOnlyList<ProductDto>> GetActiveCatalogAsync(CancellationToken ct);
}
