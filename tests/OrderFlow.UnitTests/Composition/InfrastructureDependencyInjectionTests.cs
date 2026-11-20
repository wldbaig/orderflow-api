using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Intelligence;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Intelligence;
using Xunit;

namespace OrderFlow.UnitTests.Composition;

public sealed class InfrastructureDependencyInjectionTests
{
    private const string ConnectionString = "Server=.;Database=OrderFlowTests;Trusted_Connection=True;TrustServerCertificate=True;";

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> Settings(string? provider = null, string? openAiKey = null, string? geminiKey = null)
    {
        var settings = new Dictionary<string, string?> { ["ConnectionStrings:Default"] = ConnectionString };
        if (provider is not null) settings["Ai:Provider"] = provider;
        if (openAiKey is not null) settings["Ai:OpenAI:ApiKey"] = openAiKey;
        if (geminiKey is not null) settings["Ai:Gemini:ApiKey"] = geminiKey;
        return settings;
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Build(new Dictionary<string, string?>()));

        Assert.Contains("Default", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_ByDefault_UsesHeuristicProvider()
    {
        using var provider = Build(Settings());

        Assert.IsType<HeuristicOrderIntelligenceService>(provider.GetRequiredService<IOrderIntelligenceService>());
    }

    [Fact]
    public void AddInfrastructure_WithOpenAiKey_UsesOpenAiProvider()
    {
        using var provider = Build(Settings("OpenAI", openAiKey: "test-key"));

        Assert.IsType<OpenAiOrderIntelligenceService>(provider.GetRequiredService<IOrderIntelligenceService>());
    }

    [Fact]
    public void AddInfrastructure_WithGeminiKey_UsesGeminiProvider()
    {
        using var provider = Build(Settings("gemini", geminiKey: "test-key"));

        Assert.IsType<GeminiOrderIntelligenceService>(provider.GetRequiredService<IOrderIntelligenceService>());
    }

    [Theory]
    [InlineData("OpenAI")]
    [InlineData("Gemini")]
    public void AddInfrastructure_WithProviderButNoKey_FallsBackToHeuristic(string aiProvider)
    {
        using var provider = Build(Settings(aiProvider));

        Assert.IsType<HeuristicOrderIntelligenceService>(provider.GetRequiredService<IOrderIntelligenceService>());
    }
}
