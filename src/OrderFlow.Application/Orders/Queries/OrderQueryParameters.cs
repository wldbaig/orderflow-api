using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Orders.Queries;

/// <summary>
/// The single, validated shape used for every order list query — the regular
/// <c>GET /api/orders</c> endpoint AND the AI natural-language search. Because both
/// paths funnel through this same object (and its validator), the database only ever
/// sees safe, bounded, whitelisted parameters.
/// </summary>
public sealed class OrderQueryParameters
{
    public const int MaxPageSize = 200;

    private int _page = 1;
    private int _pageSize = 25;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    // --- Filters (all optional) ---
    public OrderStatus? Status { get; set; }
    public OrderPriority? Priority { get; set; }
    public string? CustomerName { get; set; }
    public int? MinTotalUnits { get; set; }
    public int? MaxTotalUnits { get; set; }
    public decimal? MinTotalAmount { get; set; }
    public decimal? MaxTotalAmount { get; set; }
    public DateTime? CreatedAfterUtc { get; set; }
    public DateTime? CreatedBeforeUtc { get; set; }

    /// <summary>Case-insensitive substring match against an order's AI risk flags.</summary>
    public string? RiskFlagContains { get; set; }

    // --- Sorting ---
    public OrderSortField SortBy { get; set; } = OrderSortField.CreatedAtUtc;
    public bool SortDescending { get; set; } = true;
}
