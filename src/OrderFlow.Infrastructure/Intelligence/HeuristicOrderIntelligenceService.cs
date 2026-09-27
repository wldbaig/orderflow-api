using System.Text.RegularExpressions;
using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Infrastructure.Intelligence;

/// <summary>
/// Offline, rule-based implementation of the AI port. It is NOT a language model — it is a
/// deterministic keyword heuristic that lets the whole flow (intake analysis + NL search)
/// run and demo with no API key or network. Swap <c>Ai:Provider</c> to <c>OpenAI</c> or
/// <c>Gemini</c> for real model-backed results. The interface and behaviour are identical,
/// which is the point: the rest of the system does not know or care which one is wired up.
/// </summary>
public sealed class HeuristicOrderIntelligenceService : IOrderIntelligenceService
{
    private const int LargeQuantityThreshold = 1000;

    public Task<OrderIntelligenceResult> AnalyzeOrderAsync(OrderIntelligenceRequest request, CancellationToken ct)
    {
        var text = (request.SpecialInstructions ?? string.Empty).ToLowerInvariant();
        var flags = new List<string>();

        if (Contains(text, "cash on delivery", "c.o.d", " cod ", "cod.", "pay on delivery"))
            flags.Add("Requests cash on delivery");
        if (Contains(text, "discount", "free of charge", "waive", "price match", "for free"))
            flags.Add("Pricing concession requested");
        if (Contains(text, "urgent", "asap", "rush", "same day", "same-day", "today", "immediately", "overnight"))
            flags.Add("Tight delivery deadline");
        if (Contains(text, "new address", "different address", "change address", "unverified", "temporary address"))
            flags.Add("Delivery to unverified address");
        if (request.TotalUnits > LargeQuantityThreshold)
            flags.Add($"Unusually large quantity ({request.TotalUnits} units)");

        var priority = DerivePriority(text, request.TotalUnits, flags.Count);
        var summary = BuildSummary(request, flags);

        return Task.FromResult(new OrderIntelligenceResult(true, summary, priority, flags));
    }

    public Task<NlFilterTranslation> TranslateSearchAsync(string phrase, CancellationToken ct)
    {
        var text = (phrase ?? string.Empty).ToLowerInvariant();
        var p = new OrderQueryParameters();
        var notes = new List<string>();

        // Priority
        if (Contains(text, "high priority", "high-priority", "urgent")) { p.Priority = OrderPriority.High; notes.Add("priority = High"); }
        else if (Contains(text, "low priority", "low-priority")) { p.Priority = OrderPriority.Low; notes.Add("priority = Low"); }
        else if (Contains(text, "medium priority")) { p.Priority = OrderPriority.Medium; notes.Add("priority = Medium"); }

        // Status
        if (Contains(text, "cancelled", "canceled")) { p.Status = OrderStatus.Cancelled; notes.Add("status = Cancelled"); }
        else if (Contains(text, "pending")) { p.Status = OrderStatus.Pending; notes.Add("status = Pending"); }
        else if (Contains(text, "confirmed")) { p.Status = OrderStatus.Confirmed; notes.Add("status = Confirmed"); }
        else if (Contains(text, "shipped")) { p.Status = OrderStatus.Shipped; notes.Add("status = Shipped"); }
        else if (Contains(text, "completed")) { p.Status = OrderStatus.Completed; notes.Add("status = Completed"); }

        // Relative dates
        var now = DateTime.UtcNow;
        if (Contains(text, "last week", "past week", "last 7 days")) { p.CreatedAfterUtc = now.AddDays(-7); notes.Add("created in the last 7 days"); }
        else if (Contains(text, "last month", "past month", "last 30 days")) { p.CreatedAfterUtc = now.AddDays(-30); notes.Add("created in the last 30 days"); }
        else if (Contains(text, "yesterday")) { p.CreatedAfterUtc = now.Date.AddDays(-1); p.CreatedBeforeUtc = now.Date; notes.Add("created yesterday"); }
        else if (Contains(text, "today")) { p.CreatedAfterUtc = now.Date; notes.Add("created today"); }

        // Unit thresholds
        var overUnits = MatchInt(text, @"(?:over|more than|above|greater than)\s+(\d+)\s*units");
        if (overUnits is not null) { p.MinTotalUnits = overUnits; notes.Add($"units > {overUnits}"); }
        var underUnits = MatchInt(text, @"(?:under|less than|below|fewer than)\s+(\d+)\s*units");
        if (underUnits is not null) { p.MaxTotalUnits = underUnits; notes.Add($"units < {underUnits}"); }

        // Amount thresholds ($)
        var overAmount = MatchDecimal(text, @"(?:over|more than|above)\s*\$\s*(\d+(?:\.\d+)?)");
        if (overAmount is not null) { p.MinTotalAmount = overAmount; notes.Add($"amount > {overAmount}"); }
        var underAmount = MatchDecimal(text, @"(?:under|less than|below)\s*\$\s*(\d+(?:\.\d+)?)");
        if (underAmount is not null) { p.MaxTotalAmount = underAmount; notes.Add($"amount < {underAmount}"); }

        // Risk flags
        if (Contains(text, "cash on delivery", "cod")) { p.RiskFlagContains = "cash on delivery"; notes.Add("flagged: cash on delivery"); }

        // Sorting
        if (Contains(text, "largest", "biggest", "highest value", "most valuable")) { p.SortBy = OrderSortField.TotalAmount; p.SortDescending = true; notes.Add("sorted by amount, high to low"); }
        else if (Contains(text, "most units", "largest quantity")) { p.SortBy = OrderSortField.TotalUnits; p.SortDescending = true; notes.Add("sorted by units, high to low"); }
        else if (Contains(text, "oldest")) { p.SortBy = OrderSortField.CreatedAtUtc; p.SortDescending = false; notes.Add("sorted oldest first"); }
        else if (Contains(text, "newest", "recent", "latest")) { p.SortBy = OrderSortField.CreatedAtUtc; p.SortDescending = true; notes.Add("sorted newest first"); }

        var interpretation = notes.Count == 0
            ? "No specific filters detected; returning the most recent orders."
            : "Interpreted as: " + string.Join(", ", notes) + ".";

        return Task.FromResult(new NlFilterTranslation(true, p, interpretation));
    }

    private static OrderPriority DerivePriority(string text, int totalUnits, int flagCount)
    {
        if (Contains(text, "urgent", "asap", "rush", "same day", "same-day", "immediately", "critical", "overnight")
            || totalUnits > LargeQuantityThreshold || flagCount >= 2)
            return OrderPriority.High;
        if (Contains(text, "no rush", "whenever", "not urgent", "flexible", "standard", "no hurry"))
            return OrderPriority.Low;
        return OrderPriority.Medium;
    }

    private static string BuildSummary(OrderIntelligenceRequest r, IReadOnlyList<string> flags)
    {
        var basis = string.IsNullOrWhiteSpace(r.SpecialInstructions)
            ? "No special instructions provided."
            : Truncate(r.SpecialInstructions.Trim(), 140);

        var flagNote = flags.Count == 0 ? "" : $" [{flags.Count} risk flag(s)]";
        return $"{r.TotalUnits} units across {r.LineCount} line(s) for {r.CustomerName}. {basis}{flagNote}";
    }

    private static bool Contains(string text, params string[] terms) => terms.Any(text.Contains);

    private static int? MatchInt(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    private static decimal? MatchDecimal(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success && decimal.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
