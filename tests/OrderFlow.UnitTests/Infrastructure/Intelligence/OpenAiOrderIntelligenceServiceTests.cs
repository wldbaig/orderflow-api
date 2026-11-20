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

public sealed class OpenAiOrderIntelligenceServiceTests
{
    private static readonly OrderIntelligenceRequest Request = new OrderIntelligenceRequest("Acme", "ASAP", 5, 50m, 1);

    private static OpenAiOrderIntelligenceService CreateService(StubHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/v1/") };
        return new OpenAiOrderIntelligenceService(client, Options.Create(new AiOptions()), NullLogger<OpenAiOrderIntelligenceService>.Instance);
    }

    private static string Completion(string content) =>
        "{\"choices\":[{\"message\":{\"content\":" + JsonSerializer.Serialize(content) + "}}]}";

    [Fact]
    public async Task AnalyzeOrder_WithSuccessfulResponse_ParsesResult()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Completion("""{"summary":"Rush","priority":"High","riskFlags":["COD"]}"""));

        var result = await CreateService(handler).AnalyzeOrderAsync(Request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderPriority.High, result.Priority);
        Assert.Equal("/v1/chat/completions", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AnalyzeOrder_WhenProviderErrors_ReturnsFailedInsteadOfThrowing()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");

        var result = await CreateService(handler).AnalyzeOrderAsync(Request, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AnalyzeOrder_WhenResponseShapeIsUnexpected_ReturnsFailed()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, """{"unexpected":true}""");

        var result = await CreateService(handler).AnalyzeOrderAsync(Request, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task TranslateSearch_WithSuccessfulResponse_ParsesFilters()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Completion("""{"status":"Cancelled"}"""));

        var result = await CreateService(handler).TranslateSearchAsync("cancelled", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Cancelled, result.Parameters.Status);
    }

    [Fact]
    public async Task TranslateSearch_WhenProviderErrors_ReturnsUnsuccessful()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await CreateService(handler).TranslateSearchAsync("cancelled", CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
