using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Seed;

namespace OrderFlow.Api.Startup;

/// <summary>
/// Applies EF migrations at startup and optionally seeds demo data. Controlled by the
/// "Database:ApplyMigrationsOnStartup" and "Database:SeedOnStartup"/"Database:SeedOrderCount"
/// settings so this is safe to disable in real deployments.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        var config = app.Configuration.GetSection("Database");
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        var db = sp.GetRequiredService<OrderFlowDbContext>();

        if (config.GetValue("ApplyMigrationsOnStartup", true))
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
        }

        if (config.GetValue("SeedOnStartup", true))
        {
            var count = config.GetValue("SeedOrderCount", DataSeeder.DefaultOrderCount);
            var seeder = sp.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync(count);
        }
    }
}
