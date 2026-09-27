using MediatR;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderLineInput(string ProductSku, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>Command to place a new order. Returns the full created order (including AI results).</summary>
public sealed record CreateOrderCommand(
    string CustomerName,
    string? CustomerReference,
    string? SpecialInstructions,
    IReadOnlyList<CreateOrderLineInput> Lines) : IRequest<OrderDetailDto>;
