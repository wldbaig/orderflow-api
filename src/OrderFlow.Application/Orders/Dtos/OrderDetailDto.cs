using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Orders.Dtos;

/// <summary>Full order representation returned by create and get-by-id.</summary>
public sealed class OrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerReference { get; init; }
    public string SpecialInstructions { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public OrderPriority Priority { get; init; }
    public decimal TotalAmount { get; init; }
    public int TotalUnits { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }

    // AI intake results
    public AiAnalysisStatus AiAnalysisStatus { get; init; }
    public string? AiSummary { get; init; }
    public IReadOnlyList<string> AiRiskFlags { get; init; } = Array.Empty<string>();
    public DateTime? AiAnalyzedAtUtc { get; init; }

    public IReadOnlyList<OrderLineDto> Lines { get; init; } = Array.Empty<OrderLineDto>();
}
