using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Intelligence;

namespace OrderFlow.Infrastructure.Intelligence;

/// <summary>
/// Google Gemini-backed implementation. Same resilience and never-throw-on-analyze contract
/// as the OpenAI client — the two are interchangeable behind <see cref="IOrderIntelligenceService"/>.
/// </summary>
public sealed class GeminiOrderIntelligenceService : IOrderIntelligenceService
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiOrderIntelligenceService> _logger;

    public GeminiOrderIntelligenceService(HttpClient http, IOptions<AiOptions> options, ILogger<GeminiOrderIntelligenceService> logger)
    {
        _http = http;
        _options = options.Value.Gemini;
        _logger = logger;
    }

    public async Task<OrderIntelligenceResult> AnalyzeOrderAsync(OrderIntelligenceRequest request, CancellationToken ct)
    {
        try
        {
            var content = await GenerateAsync(IntelligencePrompts.AnalysisSystem, IntelligencePrompts.BuildAnalysisUser(request), ct);
            return IntelligencePrompts.ParseAnalysis(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini order analysis failed; returning unprocessed result.");
            return OrderIntelligenceResult.Failed();
        }
    }

    public async Task<NlFilterTranslation> TranslateSearchAsync(string phrase, CancellationToken ct)
    {
        try
        {
            var system = IntelligencePrompts.BuildSearchSystem(DateTime.UtcNow);
            var content = await GenerateAsync(system, IntelligencePrompts.BuildSearchUser(phrase), ct);
            return IntelligencePrompts.ParseFilter(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini search translation failed.");
            return new NlFilterTranslation(false, new Application.Orders.Queries.OrderQueryParameters(), null);
        }
    }

    private async Task<string?> GenerateAsync(string system, string user, CancellationToken ct)
    {
        // Key is passed as a query parameter per the Gemini REST contract; it stays out of source.
        var url = $"models/{_options.Model}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = system } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = user } } } },
            generationConfig = new { temperature = 0, responseMimeType = "application/json" }
        };

        using var response = await _http.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();
    }
}
