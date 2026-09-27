namespace OrderFlow.Infrastructure.Intelligence;

/// <summary>
/// AI configuration, bound from the "Ai" section. The <see cref="Provider"/> selects which
/// implementation of <c>IOrderIntelligenceService</c> is wired up, so the client can switch
/// between OpenAI, Gemini, or the offline heuristic without code changes. Keys come from
/// configuration/secrets and are never hard-coded.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>One of: <c>OpenAI</c>, <c>Gemini</c>, <c>Heuristic</c> (default, offline).</summary>
    public string Provider { get; set; } = "Heuristic";

    public int TimeoutSeconds { get; set; } = 15;
    public int MaxRetries { get; set; } = 2;

    public OpenAiOptions OpenAI { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}
