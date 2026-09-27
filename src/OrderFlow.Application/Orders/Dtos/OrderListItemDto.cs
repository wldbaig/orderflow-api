using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Orders.Dtos;

/// <summary>
/// Flat projection for the orders list. The read service projects EF queries straight
/// into this type (SELECT only these columns) — the full Order aggregate and its lines
/// are never materialised for a list query, avoiding N+1 and over-fetching.
/// </summary>
public sealed class OrderListItemDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public OrderPriority Priority { get; init; }
    public decimal TotalAmount { get; init; }
    public int TotalUnits { get; init; }
    public int LineCount { get; init; }
    public AiAnalysisStatus AiAnalysisStatus { get; init; }
    public string? AiSummary { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
