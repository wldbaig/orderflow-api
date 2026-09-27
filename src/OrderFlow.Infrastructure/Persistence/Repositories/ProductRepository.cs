using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Reference.Dtos;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly OrderFlowDbContext _db;

    public ProductRepository(OrderFlowDbContext db) => _db = db;

    public async Task AddAsync(Product product, CancellationToken ct) =>
        await _db.Products.AddAsync(product, ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct) =>
        _db.Products.AsNoTracking().AnyAsync(p => p.Sku == sku, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    // Projected, no-tracking read of just the fields the catalog exposes.
    public async Task<IReadOnlyList<ProductDto>> GetActiveCatalogAsync(CancellationToken ct) =>
        await _db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Sku = p.Sku,
                Name = p.Name,
                Category = p.Category,
                UnitPrice = p.UnitPrice
            })
            .ToListAsync(ct);
}
