using System.Text.Json;
using System.Text.Json.Serialization;
using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Infrastructure.Intelligence;

/// <summary>
/// Shared prompt construction and strict, defensive parsing of model output. Both the
/// OpenAI and Gemini clients use this so the prompt contract and the output-guarding logic
/// live in exactly one place. Nothing here trusts the model blindly: every field is parsed
/// into a constrained type and out-of-range values are dropped or clamped.
/// </summary>
public static class IntelligencePrompts
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ---------- Order analysis ----------

    public const string AnalysisSystem =
        "You are an order-intake assistant for a B2B distributor. Read the order's free-text " +
        "special instructions and return STRICT JSON only, no prose, matching this shape: " +
        "{\"summary\": string (max 200 chars), \"priority\": \"Low\"|\"Medium\"|\"High\", " +
        "\"riskFlags\": string[] (short phrases)}. " +
        "Raise risk flags for things a human should double-check, e.g. requests for cash on " +
        "delivery, unusually large quantities, discount/pricing demands, tight or same-day " +
        "deadlines, or delivery to an unverified address. If nothing is notable, return an empty array.";

    public static string BuildAnalysisUser(OrderIntelligenceRequest r) =>
        $"Customer: {r.CustomerName}\nLine count: {r.LineCount}\nTotal units: {r.TotalUnits}\n" +
        $"Total amount: {r.TotalAmount:0.00}\nSpecial instructions: \"\"\"{r.SpecialInstructions}\"\"\"";

    public static OrderIntelligenceResult ParseAnalysis(string? content)
    {
        var json = ExtractJson(content);
        if (json is null) return OrderIntelligenceResult.Failed();

        try
        {
            var dto = JsonSerializer.Deserialize<AnalysisDto>(json, Json);
            if (dto is null) return OrderIntelligenceResult.Failed();

            var priority = Enum.TryParse<OrderPriority>(dto.Priority, ignoreCase: true, out var p)
                ? p
                : OrderPriority.Medium;

            var summary = (dto.Summary ?? string.Empty).Trim();
            if (summary.Length > 200) summary = summary[..200];

            var flags = (dto.RiskFlags ?? new List<string>())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.Trim().Length > 120 ? f.Trim()[..120] : f.Trim())
                .Take(10)
                .ToList();

            return new OrderIntelligenceResult(true, summary, priority, flags);
        }
        catch (JsonException)
        {
            return OrderIntelligenceResult.Failed();
        }
    }

    // ---------- Natural-language search translation ----------

    public static string BuildSearchSystem(DateTime utcNow) =>
        "You convert a natural-language order-search phrase into STRICT JSON filter parameters. " +
        "Return JSON only, no prose. Allowed fields (all optional): " +
        "status (Pending|Confirmed|Shipped|Completed|Cancelled), " +
        "priority (Low|Medium|High), customerName (string), " +
        "minTotalUnits (int), maxTotalUnits (int), minTotalAmount (number), maxTotalAmount (number), " +
        "createdAfterUtc (ISO-8601 UTC), createdBeforeUtc (ISO-8601 UTC), riskFlagContains (string), " +
        "sortBy (CreatedAtUtc|TotalAmount|TotalUnits|Priority|OrderNumber|CustomerName|Status), " +
        "sortDescending (bool), interpretation (one short sentence describing how you read the phrase). " +
        $"Today is {utcNow:yyyy-MM-dd} UTC; resolve relative dates (e.g. 'last week') against it. " +
        "Never invent filters that are not implied by the phrase.";

    public static string BuildSearchUser(string phrase) => $"Phrase: \"\"\"{phrase}\"\"\"";

    public static NlFilterTranslation ParseFilter(string? content)
    {
        var json = ExtractJson(content);
        if (json is null) return Fail();

        try
        {
            var dto = JsonSerializer.Deserialize<FilterDto>(json, Json);
            if (dto is null) return Fail();

            var p = new OrderQueryParameters
            {
                CustomerName = Clean(dto.CustomerName),
                RiskFlagContains = Clean(dto.RiskFlagContains),
                MinTotalUnits = dto.MinTotalUnits,
                MaxTotalUnits = dto.MaxTotalUnits,
                MinTotalAmount = dto.MinTotalAmount,
                MaxTotalAmount = dto.MaxTotalAmount,
                CreatedAfterUtc = dto.CreatedAfterUtc,
                CreatedBeforeUtc = dto.CreatedBeforeUtc,
                SortDescending = dto.SortDescending ?? true
            };

            if (Enum.TryParse<OrderStatus>(dto.Status, ignoreCase: true, out var s)) p.Status = s;
            if (Enum.TryParse<OrderPriority>(dto.Priority, ignoreCase: true, out var pr)) p.Priority = pr;
            if (Enum.TryParse<OrderSortField>(dto.SortBy, ignoreCase: true, out var sort)) p.SortBy = sort;

            return new NlFilterTranslation(true, p, Clean(dto.Interpretation));
        }
        catch (JsonException)
        {
            return Fail();
        }
    }

    private static NlFilterTranslation Fail() =>
        new(false, new OrderQueryParameters(), null);

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Pulls the first JSON object out of the model's reply, tolerating ``` fences and prose.</summary>
    private static string? ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        return start >= 0 && end > start ? content.Substring(start, end - start + 1) : null;
    }

    private sealed class AnalysisDto
    {
        public string? Summary { get; set; }
        public string? Priority { get; set; }
        public List<string>? RiskFlags { get; set; }
    }

    private sealed class FilterDto
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? CustomerName { get; set; }
        public int? MinTotalUnits { get; set; }
        public int? MaxTotalUnits { get; set; }
        public decimal? MinTotalAmount { get; set; }
        public decimal? MaxTotalAmount { get; set; }
        public DateTime? CreatedAfterUtc { get; set; }
        public DateTime? CreatedBeforeUtc { get; set; }
        public string? RiskFlagContains { get; set; }
        public string? SortBy { get; set; }
        public bool? SortDescending { get; set; }
        public string? Interpretation { get; set; }
    }
}
