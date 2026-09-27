using OrderFlow.Domain.Common;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// The Order aggregate root. All order business rules live here:
/// totals are computed in the domain, status transitions are guarded,
/// and AI-derived intelligence is applied through an explicit method.
/// Controllers and EF queries never mutate this state directly.
/// </summary>
public class Order
{
    private readonly List<OrderLine> _lines = new();

    // EF Core materialisation constructor.
    private Order()
    {
        OrderNumber = string.Empty;
        CustomerName = string.Empty;
        SpecialInstructions = string.Empty;
    }

    private Order(string orderNumber, string customerName, string? customerReference, string? specialInstructions)
    {
        Id = Guid.NewGuid();
        OrderNumber = orderNumber;
        CustomerName = customerName;
        CustomerReference = customerReference?.Trim();
        SpecialInstructions = specialInstructions?.Trim() ?? string.Empty;
        Status = OrderStatus.Pending;
        Priority = OrderPriority.Medium;
        AiAnalysisStatus = AiAnalysisStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>Human-friendly, unique order reference (e.g. <c>ORD-2026-000123</c>).</summary>
    public string OrderNumber { get; private set; }

    public string CustomerName { get; private set; }
    public string? CustomerReference { get; private set; }

    /// <summary>Free-text instructions from the customer — the input to the AI intake step.</summary>
    public string SpecialInstructions { get; private set; }

    public OrderStatus Status { get; private set; }
    public OrderPriority Priority { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // --- AI intake results (persisted on the order) ---
    public AiAnalysisStatus AiAnalysisStatus { get; private set; }
    public string? AiSummary { get; private set; }
    /// <summary>Risk flags stored as a newline-separated list (mapped to a collection at the edge).</summary>
    public string? AiRiskFlags { get; private set; }
    public DateTime? AiAnalyzedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Order total. Computed in the domain (never in a controller) and persisted as a real
    /// column so the read side can filter and sort on it against an index — no per-row
    /// subquery over the lines table.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Total units across all lines — the value the "unusual quantity" risk check reasons
    /// about. Also persisted for index-friendly filtering/sorting.
    /// </summary>
    public int TotalUnits { get; private set; }

    /// <summary>Factory. Enforces that an order is created in a valid, consistent state.</summary>
    public static Order Create(
        string orderNumber,
        string customerName,
        string? customerReference,
        string? specialInstructions,
        IEnumerable<(string Sku, string Name, int Quantity, decimal UnitPrice)> lines)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Order number is required.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");

        var order = new Order(orderNumber.Trim(), customerName.Trim(), customerReference, specialInstructions);

        foreach (var line in lines)
            order.AddLine(line.Sku, line.Name, line.Quantity, line.UnitPrice);

        if (order._lines.Count == 0)
            throw new DomainException("An order must have at least one line.");

        return order;
    }

    public void AddLine(string productSku, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Lines can only be added while the order is Pending.");

        _lines.Add(new OrderLine(productSku, productName, quantity, unitPrice));
        Recalculate();
        Touch();
    }

    /// <summary>Recomputes the persisted totals from the current lines.</summary>
    private void Recalculate()
    {
        TotalAmount = _lines.Sum(l => l.LineTotal);
        TotalUnits = _lines.Sum(l => l.Quantity);
    }

    /// <summary>Business rule: an order can only be cancelled while it is still Pending.</summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Only Pending orders can be cancelled. This order is {Status}.");

        Status = OrderStatus.Cancelled;
        Touch();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Only Pending orders can be confirmed. This order is {Status}.");

        Status = OrderStatus.Confirmed;
        Touch();
    }

    /// <summary>
    /// Applies the structured result of the AI intake analysis. Called from the
    /// order-create flow after a successful AI call. Priority is a business
    /// attribute, so it becomes the order's priority.
    /// </summary>
    public void ApplyIntelligence(string summary, OrderPriority priority, IEnumerable<string> riskFlags)
    {
        AiSummary = summary?.Trim();
        Priority = priority;
        var flags = riskFlags?.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToArray()
                    ?? Array.Empty<string>();
        AiRiskFlags = flags.Length == 0 ? null : string.Join('\n', flags);
        AiAnalysisStatus = AiAnalysisStatus.Processed;
        AiAnalyzedAtUtc = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Marks the AI analysis as failed. The order remains fully valid and usable;
    /// this only records that the intelligence fields could not be populated.
    /// </summary>
    public void MarkIntelligenceFailed()
    {
        AiAnalysisStatus = AiAnalysisStatus.Failed;
        AiAnalyzedAtUtc = DateTime.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
