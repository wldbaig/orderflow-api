using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.Queries.GetOrders;

/// <summary>
/// Thin query handler: validated parameters in, projected page out. All the EF projection,
/// paging and index-friendly sorting lives in <see cref="IOrderReadService"/> (Infrastructure).
/// </summary>
public sealed class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    private readonly IOrderReadService _readService;

    public GetOrdersQueryHandler(IOrderReadService readService) => _readService = readService;

    public Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken ct) =>
        _readService.ListOrdersAsync(request.Parameters, ct);
}
