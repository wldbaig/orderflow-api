using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Intelligence;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Intelligence;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Intelligence;

public sealed class GeminiOrderIntelligenceServiceTests
{
    private static readonly OrderIntelligenceRequest Request = new OrderIntelligenceRequest("Acme", "ASAP", 5, 50m, 1);

    private static GeminiOrderIntelligenceService CreateService(StubHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://gemini.test/v1beta/") };
        return new GeminiOrderIntelligenceService(client, Options.Create(new AiOptions()), NullLogger<GeminiOrderIntelligenceService>.Instance);
    }

    private static string Candidate(string text) =>
        "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":" + JsonSerializer.Serialize(text) + "}]}}]}";

    [Fact]
    public async Task AnalyzeOrder_WithSuccessfulResponse_ParsesResult()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Candidate("""{"summary":"Rush","priority":"Low","riskFlags":[]}"""));

        var result = await CreateService(handler).AnalyzeOrderAsync(Request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderPriority.Low, result.Priority);
        Assert.Equal("/v1beta/models/gemini-1.5-flash:generateContent", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AnalyzeOrder_WhenProviderErrors_ReturnsFailedInsteadOfThrowing()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.TooManyRequests, "{}");

        var result = await CreateService(handler).AnalyzeOrderAsync(Request, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task TranslateSearch_WithSuccessfulResponse_ParsesFilters()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Candidate("""{"priority":"High"}"""));

        var result = await CreateService(handler).TranslateSearchAsync("urgent", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderPriority.High, result.Parameters.Priority);
    }

    [Fact]
    public async Task TranslateSearch_WhenProviderErrors_ReturnsUnsuccessful()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.BadGateway, "{}");

        var result = await CreateService(handler).TranslateSearchAsync("urgent", CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
