namespace OrderFlow.Application.Intelligence;

/// <summary>
/// The context handed to the AI intake step. Deliberately a small, explicit payload
/// (not the whole entity) so the boundary to the external model is clear and reviewable.
/// </summary>
public sealed record OrderIntelligenceRequest(
    string CustomerName,
    string SpecialInstructions,
    int TotalUnits,
    decimal TotalAmount,
    int LineCount);
