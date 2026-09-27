using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// Read-side (query) access to orders. Implementations project directly to DTOs with
/// <c>AsNoTracking</c> and server-side paging/filtering/sorting — they must not load
/// full aggregates. This is the performance-critical read path.
/// </summary>
public interface IOrderReadService
{
    Task<PagedResult<OrderListItemDto>> ListOrdersAsync(OrderQueryParameters parameters, CancellationToken ct);

    Task<OrderDetailDto?> GetOrderDetailAsync(Guid id, CancellationToken ct);
}
