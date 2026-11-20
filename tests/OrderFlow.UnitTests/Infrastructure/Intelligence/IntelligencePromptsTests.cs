using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Intelligence;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Intelligence;

public sealed class IntelligencePromptsTests
{
    [Fact]
    public void ParseAnalysis_WithValidJson_ReturnsStructuredResult()
    {
        var result = IntelligencePrompts.ParseAnalysis("""{"summary":" Rush ","priority":"high","riskFlags":["COD"," ",""]}""");

        Assert.True(result.Succeeded);
        Assert.Equal("Rush", result.Summary);
        Assert.Equal(OrderPriority.High, result.Priority);
        Assert.Equal(new[] { "COD" }, result.RiskFlags);
    }

    [Fact]
    public void ParseAnalysis_WithJsonWrappedInProse_ExtractsJson()
    {
        var result = IntelligencePrompts.ParseAnalysis("""Sure! {"summary":"ok","priority":"Low","riskFlags":[]} Hope that helps.""");

        Assert.True(result.Succeeded);
        Assert.Equal(OrderPriority.Low, result.Priority);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no json here")]
    [InlineData("{not valid json}")]
    public void ParseAnalysis_WithUnusableContent_ReturnsFailed(string? content)
    {
        Assert.False(IntelligencePrompts.ParseAnalysis(content).Succeeded);
    }

    [Fact]
    public void ParseAnalysis_WithUnknownPriority_DefaultsToMedium()
    {
        var result = IntelligencePrompts.ParseAnalysis("""{"summary":"x","priority":"Extreme","riskFlags":[]}""");

        Assert.Equal(OrderPriority.Medium, result.Priority);
    }

    [Fact]
    public void ParseAnalysis_ClampsSummaryFlagCountAndFlagLength()
    {
        var flags = string.Join(",", Enumerable.Range(0, 15).Select(i => $"\"flag {i}\""));
        var longFlag = new string('f', 150);
        var json = $$"""{"summary":"{{new string('s', 250)}}","priority":"Low","riskFlags":["{{longFlag}}",{{flags}}]}""";

        var result = IntelligencePrompts.ParseAnalysis(json);

        Assert.Equal(200, result.Summary.Length);
        Assert.Equal(10, result.RiskFlags.Count);
        Assert.Equal(120, result.RiskFlags[0].Length);
    }

    [Fact]
    public void ParseFilter_WithValidJson_MapsAllFields()
    {
        var json = """
        {"status":"Pending","priority":"High","customerName":"  Acme ","minTotalUnits":10,"maxTotalUnits":20,
         "minTotalAmount":5.5,"maxTotalAmount":99,"createdAfterUtc":"2026-01-01T00:00:00Z","createdBeforeUtc":"2026-02-01T00:00:00Z",
         "riskFlagContains":"cash","sortBy":"TotalAmount","sortDescending":false,"interpretation":" High pending "}
        """;

        var result = IntelligencePrompts.ParseFilter(json);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Pending, result.Parameters.Status);
        Assert.Equal(OrderPriority.High, result.Parameters.Priority);
        Assert.Equal("Acme", result.Parameters.CustomerName);
        Assert.Equal(10, result.Parameters.MinTotalUnits);
        Assert.Equal(20, result.Parameters.MaxTotalUnits);
        Assert.Equal(5.5m, result.Parameters.MinTotalAmount);
        Assert.Equal(99m, result.Parameters.MaxTotalAmount);
        Assert.NotNull(result.Parameters.CreatedAfterUtc);
        Assert.NotNull(result.Parameters.CreatedBeforeUtc);
        Assert.Equal("cash", result.Parameters.RiskFlagContains);
        Assert.Equal(OrderSortField.TotalAmount, result.Parameters.SortBy);
        Assert.False(result.Parameters.SortDescending);
        Assert.Equal("High pending", result.Interpretation);
    }

    [Fact]
    public void ParseFilter_IgnoresUnknownEnumValues()
    {
        var result = IntelligencePrompts.ParseFilter("""{"status":"Lost","priority":"Mega","sortBy":"Colour"}""");

        Assert.True(result.Succeeded);
        Assert.Null(result.Parameters.Status);
        Assert.Null(result.Parameters.Priority);
        Assert.Equal(OrderSortField.CreatedAtUtc, result.Parameters.SortBy);
        Assert.True(result.Parameters.SortDescending);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nothing")]
    [InlineData("{broken")]
    public void ParseFilter_WithUnusableContent_ReturnsFailed(string? content)
    {
        Assert.False(IntelligencePrompts.ParseFilter(content).Succeeded);
    }

    [Fact]
    public void BuildSearchSystem_EmbedsTodaysDate()
    {
        var prompt = IntelligencePrompts.BuildSearchSystem(new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc));

        Assert.Contains("Today is 2026-11-20 UTC", prompt);
    }

    [Fact]
    public void BuildAnalysisUser_IncludesOrderFacts()
    {
        var prompt = IntelligencePrompts.BuildAnalysisUser(new OrderIntelligenceRequest("Acme", "Leave at dock", 12, 99.5m, 3));

        Assert.Contains("Customer: Acme", prompt);
        Assert.Contains("Line count: 3", prompt);
        Assert.Contains("Total units: 12", prompt);
        Assert.Contains("Leave at dock", prompt);
    }

    [Fact]
    public void BuildSearchUser_WrapsPhrase()
    {
        Assert.Equal("Phrase: \"\"\"late orders\"\"\"", IntelligencePrompts.BuildSearchUser("late orders"));
    }
}
