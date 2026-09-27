namespace OrderFlow.Application.Intelligence;

/// <summary>
/// AI capabilities woven into the order flow. Defined in Application (a port); the
/// concrete OpenAI / Gemini clients live in Infrastructure and are chosen by config.
/// </summary>
public interface IOrderIntelligenceService
{
    /// <summary>
    /// Analyses an order's free-text special instructions and returns a summary, priority
    /// and risk flags. Must be resilient: on any failure it returns a non-succeeded result
    /// rather than throwing, so order creation always completes.
    /// </summary>
    Task<OrderIntelligenceResult> AnalyzeOrderAsync(OrderIntelligenceRequest request, CancellationToken ct);

    /// <summary>
    /// Translates a natural-language phrase into structured, un-executed query parameters.
    /// The result must still be validated by the caller before use.
    /// </summary>
    Task<NlFilterTranslation> TranslateSearchAsync(string phrase, CancellationToken ct);
}
