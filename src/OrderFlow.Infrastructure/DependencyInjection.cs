using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Intelligence;
using OrderFlow.Infrastructure.Caching;
using OrderFlow.Infrastructure.Intelligence;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Repositories;
using OrderFlow.Infrastructure.Persistence.Seed;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<OrderFlowDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(OrderFlowDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            }));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderReadService, OrderReadService>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<DataSeeder>();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName));

        AddIntelligence(services, configuration);

        return services;
    }

    private static void AddIntelligence(IServiceCollection services, IConfiguration configuration)
    {
        var ai = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        var provider = ai.Provider?.Trim();

        // Fall back to the offline heuristic if a model provider is selected without a key,
        // so the app always runs. The choice is logged at startup.
        var openAiReady = string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase)
                          && !string.IsNullOrWhiteSpace(ai.OpenAI.ApiKey);
        var geminiReady = string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase)
                          && !string.IsNullOrWhiteSpace(ai.Gemini.ApiKey);

        if (openAiReady)
        {
            services.AddHttpClient<IOrderIntelligenceService, OpenAiOrderIntelligenceService>(client =>
                {
                    client.BaseAddress = new Uri(EnsureTrailingSlash(ai.OpenAI.BaseUrl));
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ai.OpenAI.ApiKey);
                    client.Timeout = Timeout.InfiniteTimeSpan; // Polly owns per-attempt timeouts.
                })
                .AddPolicyHandler(RetryPolicy(ai.MaxRetries))
                .AddPolicyHandler(TimeoutPolicy(ai.TimeoutSeconds));
        }
        else if (geminiReady)
        {
            services.AddHttpClient<IOrderIntelligenceService, GeminiOrderIntelligenceService>(client =>
                {
                    client.BaseAddress = new Uri(EnsureTrailingSlash(ai.Gemini.BaseUrl));
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
                .AddPolicyHandler(RetryPolicy(ai.MaxRetries))
                .AddPolicyHandler(TimeoutPolicy(ai.TimeoutSeconds));
        }
        else
        {
            services.AddSingleton<IOrderIntelligenceService, HeuristicOrderIntelligenceService>();
        }
    }

    // Exponential backoff on transient HTTP errors (5xx, 408) and timeouts.
    private static IAsyncPolicy<HttpResponseMessage> RetryPolicy(int retries) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(Math.Max(0, retries), attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

    // Per-attempt timeout.
    private static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy(int seconds) =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(Math.Max(1, seconds)));

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
