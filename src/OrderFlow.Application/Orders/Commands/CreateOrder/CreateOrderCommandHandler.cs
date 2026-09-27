using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Mapping;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Orchestrates order creation. This is the flow the AI is woven INTO:
/// build the aggregate -> run AI intake analysis -> apply results (or mark failed) ->
/// persist. The AI step is best-effort and can never prevent the order from being saved.
/// </summary>
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDetailDto>
{
    private readonly IOrderRepository _repository;
    private readonly IOrderNumberGenerator _numberGenerator;
    private readonly IOrderIntelligenceService _intelligence;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IOrderNumberGenerator numberGenerator,
        IOrderIntelligenceService intelligence,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _repository = repository;
        _numberGenerator = numberGenerator;
        _intelligence = intelligence;
        _logger = logger;
    }

    public async Task<OrderDetailDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var orderNumber = await _numberGenerator.NextAsync(ct);

        // 1. Build the aggregate. All invariants (totals, non-empty lines) are enforced here.
        var order = Order.Create(
            orderNumber,
            request.CustomerName,
            request.CustomerReference,
            request.SpecialInstructions,
            request.Lines.Select(l => (l.ProductSku, l.ProductName, l.Quantity, l.UnitPrice)));

        // 2. AI intake step — resilient by contract: it returns a non-succeeded result rather
        //    than throwing, so a model outage or timeout never blocks order capture.
        var analysis = await _intelligence.AnalyzeOrderAsync(
            new OrderIntelligenceRequest(
                order.CustomerName,
                order.SpecialInstructions,
                order.TotalUnits,
                order.TotalAmount,
                order.Lines.Count),
            ct);

        if (analysis.Succeeded)
        {
            order.ApplyIntelligence(analysis.Summary, analysis.Priority, analysis.RiskFlags);
        }
        else
        {
            _logger.LogWarning("AI analysis unavailable for {OrderNumber}; saving order with unprocessed AI fields.", orderNumber);
            order.MarkIntelligenceFailed();
        }

        // 3. Persist and return the full order (AI results included).
        await _repository.AddAsync(order, ct);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Created order {OrderNumber} ({LineCount} lines, {TotalUnits} units).",
            order.OrderNumber, order.Lines.Count, order.TotalUnits);

        return order.ToDetailDto();
    }
}
