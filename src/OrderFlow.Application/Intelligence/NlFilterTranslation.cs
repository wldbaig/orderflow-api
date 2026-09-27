using OrderFlow.Application.Orders.Queries;

namespace OrderFlow.Application.Intelligence;

/// <summary>
/// Result of translating a natural-language search phrase into structured query
/// parameters. The AI only ever produces <see cref="Parameters"/> — it never touches the
/// database. Those parameters are then validated with the SAME validator as the normal
/// list endpoint before any query runs.
/// </summary>
public sealed record NlFilterTranslation(
    bool Succeeded,
    OrderQueryParameters Parameters,
    string? Interpretation);
