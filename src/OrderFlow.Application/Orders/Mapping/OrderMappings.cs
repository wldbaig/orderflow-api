using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Orders.Mapping;

/// <summary>
/// Explicit domain -> DTO mapping. Kept in one place so the boundary between the domain
/// model (inside) and the transport DTOs (edge) stays visible and deliberate.
/// </summary>
public static class OrderMappings
{
    public static OrderDetailDto ToDetailDto(this Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerName = order.CustomerName,
        CustomerReference = order.CustomerReference,
        SpecialInstructions = order.SpecialInstructions,
        Status = order.Status,
        Priority = order.Priority,
        TotalAmount = order.TotalAmount,
        TotalUnits = order.TotalUnits,
        CreatedAtUtc = order.CreatedAtUtc,
        UpdatedAtUtc = order.UpdatedAtUtc,
        AiAnalysisStatus = order.AiAnalysisStatus,
        AiSummary = order.AiSummary,
        AiRiskFlags = SplitFlags(order.AiRiskFlags),
        AiAnalyzedAtUtc = order.AiAnalyzedAtUtc,
        Lines = order.Lines.Select(l => new OrderLineDto
        {
            ProductSku = l.ProductSku,
            ProductName = l.ProductName,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.LineTotal
        }).ToArray()
    };

    /// <summary>Risk flags are stored newline-separated on the aggregate; expose them as a list at the edge.</summary>
    public static IReadOnlyList<string> SplitFlags(string? flags) =>
        string.IsNullOrWhiteSpace(flags)
            ? Array.Empty<string>()
            : flags.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
