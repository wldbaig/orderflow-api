using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Write-side repository. Loads the full tracked aggregate (with lines) so domain methods
/// like <c>Cancel()</c> can run against a consistent object graph.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderFlowDbContext _db;

    public OrderRepository(OrderFlowDbContext db) => _db = db;

    public async Task AddAsync(Order order, CancellationToken ct) =>
        await _db.Orders.AddAsync(order, ct);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
