using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Intelligence;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Intelligence;

public sealed class HeuristicOrderIntelligenceServiceTests
{
    private readonly HeuristicOrderIntelligenceService _service = new HeuristicOrderIntelligenceService();

    private static OrderIntelligenceRequest Request(string instructions, int totalUnits = 10) =>
        new OrderIntelligenceRequest("Acme Ltd", instructions, totalUnits, 100m, 2);

    [Theory]
    [InlineData("Please arrange cash on delivery", "Requests cash on delivery")]
    [InlineData("Can you waive the shipping fee", "Pricing concession requested")]
    [InlineData("Need this ASAP", "Tight delivery deadline")]
    [InlineData("Ship to a new address this time", "Delivery to unverified address")]
    public async Task AnalyzeOrder_RaisesFlagForKeyword(string instructions, string expectedFlag)
    {
        var result = await _service.AnalyzeOrderAsync(Request(instructions), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(expectedFlag, result.RiskFlags);
    }

    [Fact]
    public async Task AnalyzeOrder_WithLargeQuantity_FlagsAndRaisesPriority()
    {
        var result = await _service.AnalyzeOrderAsync(Request("", totalUnits: 1500), CancellationToken.None);

        Assert.Contains("Unusually large quantity (1500 units)", result.RiskFlags);
        Assert.Equal(OrderPriority.High, result.Priority);
    }

    [Fact]
    public async Task AnalyzeOrder_WithTwoFlags_IsHighPriority()
    {
        var result = await _service.AnalyzeOrderAsync(Request("cash on delivery and a discount please"), CancellationToken.None);

        Assert.Equal(2, result.RiskFlags.Count);
        Assert.Equal(OrderPriority.High, result.Priority);
    }

    [Fact]
    public async Task AnalyzeOrder_WithRelaxedTiming_IsLowPriority()
    {
        var result = await _service.AnalyzeOrderAsync(Request("Deliver whenever convenient"), CancellationToken.None);

        Assert.Empty(result.RiskFlags);
        Assert.Equal(OrderPriority.Low, result.Priority);
    }

    [Fact]
    public async Task AnalyzeOrder_WithoutInstructions_IsMediumWithDefaultSummary()
    {
        var result = await _service.AnalyzeOrderAsync(Request(""), CancellationToken.None);

        Assert.Empty(result.RiskFlags);
        Assert.Equal(OrderPriority.Medium, result.Priority);
        Assert.Equal("10 units across 2 line(s) for Acme Ltd. No special instructions provided.", result.Summary);
    }

    [Fact]
    public async Task AnalyzeOrder_SummaryTruncatesLongInstructionsAndCountsFlags()
    {
        var result = await _service.AnalyzeOrderAsync(Request("urgent " + new string('x', 300)), CancellationToken.None);

        Assert.Contains("…", result.Summary);
        Assert.EndsWith("[1 risk flag(s)]", result.Summary);
    }

    [Fact]
    public async Task TranslateSearch_ReadsPriorityAndStatus()
    {
        var result = await _service.TranslateSearchAsync("high priority pending orders", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderPriority.High, result.Parameters.Priority);
        Assert.Equal(OrderStatus.Pending, result.Parameters.Status);
        Assert.StartsWith("Interpreted as:", result.Interpretation);
    }

    [Fact]
    public async Task TranslateSearch_ReadsCancelledStatus()
    {
        var result = await _service.TranslateSearchAsync("cancelled orders", CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, result.Parameters.Status);
    }

    [Fact]
    public async Task TranslateSearch_ReadsUnitThresholds()
    {
        var over = await _service.TranslateSearchAsync("orders over 500 units", CancellationToken.None);
        var under = await _service.TranslateSearchAsync("orders under 20 units", CancellationToken.None);

        Assert.Equal(500, over.Parameters.MinTotalUnits);
        Assert.Equal(20, under.Parameters.MaxTotalUnits);
    }

    [Fact]
    public async Task TranslateSearch_ReadsAmountThresholds()
    {
        var over = await _service.TranslateSearchAsync("orders over $1000", CancellationToken.None);
        var under = await _service.TranslateSearchAsync("orders below $50.5", CancellationToken.None);

        Assert.Equal(1000m, over.Parameters.MinTotalAmount);
        Assert.Equal(50.5m, under.Parameters.MaxTotalAmount);
    }

    [Fact]
    public async Task TranslateSearch_ReadsSorting()
    {
        var largest = await _service.TranslateSearchAsync("largest orders", CancellationToken.None);
        var oldest = await _service.TranslateSearchAsync("oldest orders", CancellationToken.None);

        Assert.Equal(OrderSortField.TotalAmount, largest.Parameters.SortBy);
        Assert.True(largest.Parameters.SortDescending);
        Assert.Equal(OrderSortField.CreatedAtUtc, oldest.Parameters.SortBy);
        Assert.False(oldest.Parameters.SortDescending);
    }

    [Fact]
    public async Task TranslateSearch_ReadsRelativeDates()
    {
        var before = DateTime.UtcNow;

        var lastWeek = await _service.TranslateSearchAsync("orders from last week", CancellationToken.None);
        var yesterday = await _service.TranslateSearchAsync("orders from yesterday", CancellationToken.None);

        Assert.InRange(lastWeek.Parameters.CreatedAfterUtc!.Value, before.AddDays(-7).AddMinutes(-1), DateTime.UtcNow.AddDays(-7).AddMinutes(1));
        Assert.Equal(DateTime.UtcNow.Date.AddDays(-1), yesterday.Parameters.CreatedAfterUtc);
        Assert.Equal(DateTime.UtcNow.Date, yesterday.Parameters.CreatedBeforeUtc);
    }

    [Fact]
    public async Task TranslateSearch_WithNothingRecognised_ReturnsDefaults()
    {
        var result = await _service.TranslateSearchAsync("show me orders", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Parameters.Status);
        Assert.Null(result.Parameters.Priority);
        Assert.Equal("No specific filters detected; returning the most recent orders.", result.Interpretation);
    }
}
