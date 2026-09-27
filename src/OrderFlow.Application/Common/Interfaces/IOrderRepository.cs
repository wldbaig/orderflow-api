using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// Write-side access to the Order aggregate. Loads the full tracked aggregate
/// (with lines) so domain behavior can be invoked, and persists changes.
/// Read/query concerns are deliberately kept out of here — see <see cref="IOrderReadService"/>.
/// </summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);

    /// <summary>Loads a tracked aggregate including its lines, or null if not found.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
