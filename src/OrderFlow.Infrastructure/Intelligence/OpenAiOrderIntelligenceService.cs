using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Intelligence;

namespace OrderFlow.Infrastructure.Intelligence;

/// <summary>
/// OpenAI-backed implementation. Resilience (timeout + retries) is applied to the injected
/// <see cref="HttpClient"/> via Polly in DI. The analyze path swallows all failures and
/// returns a non-succeeded result, guaranteeing it can never break order creation.
/// </summary>
public sealed class OpenAiOrderIntelligenceService : IOrderIntelligenceService
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiOrderIntelligenceService> _logger;

    public OpenAiOrderIntelligenceService(HttpClient http, IOptions<AiOptions> options, ILogger<OpenAiOrderIntelligenceService> logger)
    {
        _http = http;
        _options = options.Value.OpenAI;
        _logger = logger;
    }

    public async Task<OrderIntelligenceResult> AnalyzeOrderAsync(OrderIntelligenceRequest request, CancellationToken ct)
    {
        try
        {
            var content = await CompleteAsync(IntelligencePrompts.AnalysisSystem, IntelligencePrompts.BuildAnalysisUser(request), ct);
            return IntelligencePrompts.ParseAnalysis(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI order analysis failed; returning unprocessed result.");
            return OrderIntelligenceResult.Failed();
        }
    }

    public async Task<NlFilterTranslation> TranslateSearchAsync(string phrase, CancellationToken ct)
    {
        try
        {
            var system = IntelligencePrompts.BuildSearchSystem(DateTime.UtcNow);
            var content = await CompleteAsync(system, IntelligencePrompts.BuildSearchUser(phrase), ct);
            return IntelligencePrompts.ParseFilter(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI search translation failed.");
            return new NlFilterTranslation(false, new Application.Orders.Queries.OrderQueryParameters(), null);
        }
    }

    private async Task<string?> CompleteAsync(string system, string user, CancellationToken ct)
    {
        var body = new
        {
            model = _options.Model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var response = await _http.PostAsJsonAsync("chat/completions", body, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
