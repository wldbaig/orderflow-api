using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Intelligence;

/// <summary>
/// Structured output of the AI intake analysis. <see cref="Succeeded"/> is false when the
/// model call failed or its output could not be trusted — the order-create flow uses this
/// to decide between <c>ApplyIntelligence</c> and <c>MarkIntelligenceFailed</c>. The
/// implementation never throws on the analyze path, so AI can never break order creation.
/// </summary>
public sealed record OrderIntelligenceResult(
    bool Succeeded,
    string Summary,
    OrderPriority Priority,
    IReadOnlyList<string> RiskFlags)
{
    public static OrderIntelligenceResult Failed() =>
        new(false, string.Empty, OrderPriority.Medium, Array.Empty<string>());
}
