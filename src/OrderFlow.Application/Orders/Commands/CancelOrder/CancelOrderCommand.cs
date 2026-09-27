using MediatR;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.Commands.CancelOrder;

/// <summary>Cancels an order. The "only Pending orders can be cancelled" rule lives in the domain.</summary>
public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;
