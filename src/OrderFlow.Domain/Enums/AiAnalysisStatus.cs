namespace OrderFlow.Domain.Enums;

/// <summary>
/// Tracks the outcome of the AI intake analysis for an order. The core order-create
/// flow never depends on this succeeding — a <see cref="Failed"/> order is still valid.
/// </summary>
public enum AiAnalysisStatus
{
    /// <summary>Analysis has not run yet (order created but AI step not attempted/finished).</summary>
    Pending = 0,

    /// <summary>AI produced a summary, priority and risk flags successfully.</summary>
    Processed = 1,

    /// <summary>AI call failed or timed out. Order is saved; AI fields are "unprocessed".</summary>
    Failed = 2
}
