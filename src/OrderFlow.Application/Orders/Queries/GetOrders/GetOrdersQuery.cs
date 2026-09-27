using MediatR;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.Queries.GetOrders;

/// <summary>Paged, filtered, sorted list of orders.</summary>
public sealed record GetOrdersQuery(OrderQueryParameters Parameters) : IRequest<PagedResult<OrderListItemDto>>;
