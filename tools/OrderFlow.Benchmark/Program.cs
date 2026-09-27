using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Seed;

// Reproducible read-path benchmark: NAIVE (load full tracked entities + Include, page in
// memory) vs OPTIMIZED (AsNoTracking, project to DTO, filter/sort/page server-side against
// indexes). Run with: dotnet run --project tools/OrderFlow.Benchmark -c Release

var target = args.Length > 0 && int.TryParse(args[0], out var n) ? n : DataSeeder.DefaultOrderCount;

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
var log = loggerFactory.CreateLogger("Benchmark");

// Ensure the database exists and is seeded to the target volume.
using (var setup = new OrderFlowDbContextFactory().CreateDbContext(args))
{
    log.LogInformation("Applying migrations...");
    await setup.Database.MigrateAsync();
    await new DataSeeder(setup, loggerFactory.CreateLogger<DataSeeder>()).SeedAsync(target);
    var count = await setup.Orders.CountAsync();
    log.LogInformation("Orders in database: {Count}", count);
}

const int PageSize = 25;
const int Runs = 5;

Console.WriteLine();
Console.WriteLine("Scenario 1: sort by TotalAmount desc, first page");
await Compare(
    naive: ctx =>
    {
        // Over-fetch: every order + every line, tracked, then sort & page in memory.
        var all = ctx.Orders.Include(o => o.Lines).ToList();
        return all.OrderByDescending(o => o.TotalAmount).Take(PageSize).Count();
    },
    optimized: async ctx =>
    {
        var read = new OrderReadService(ctx);
        var page = await read.ListOrdersAsync(
            new OrderQueryParameters { Page = 1, PageSize = PageSize, SortBy = OrderSortField.TotalAmount, SortDescending = true },
            default);
        return page.Items.Count;
    });

Console.WriteLine();
Console.WriteLine("Scenario 2: filter Status=Confirmed, sort CreatedAtUtc desc, first page");
await Compare(
    naive: ctx =>
    {
        var all = ctx.Orders.Include(o => o.Lines).ToList();
        return all.Where(o => o.Status == OrderStatus.Confirmed)
                  .OrderByDescending(o => o.CreatedAtUtc).Take(PageSize).Count();
    },
    optimized: async ctx =>
    {
        var read = new OrderReadService(ctx);
        var page = await read.ListOrdersAsync(
            new OrderQueryParameters { Page = 1, PageSize = PageSize, Status = OrderStatus.Confirmed, SortBy = OrderSortField.CreatedAtUtc, SortDescending = true },
            default);
        return page.Items.Count;
    });

return;

async Task Compare(Func<OrderFlowDbContext, int> naive, Func<OrderFlowDbContext, Task<int>> optimized)
{
    var naiveMs = await TimeAsync(ctx => Task.FromResult(naive(ctx)));
    var optMs = await TimeAsync(optimized);
    Console.WriteLine($"  Naive (load all + Include + in-memory paging): {naiveMs,8:0.0} ms  (median of {Runs})");
    Console.WriteLine($"  Optimized (AsNoTracking + projection + paged):  {optMs,8:0.0} ms  (median of {Runs})");
    if (optMs > 0) Console.WriteLine($"  Speed-up: {naiveMs / optMs,0:0.0}x faster");
}

async Task<double> TimeAsync(Func<OrderFlowDbContext, Task<int>> action)
{
    var samples = new List<double>();
    for (var i = 0; i < Runs + 1; i++) // first run is warm-up, discarded
    {
        using var ctx = new OrderFlowDbContextFactory().CreateDbContext(Array.Empty<string>());
        var sw = Stopwatch.StartNew();
        _ = await action(ctx);
        sw.Stop();
        if (i > 0) samples.Add(sw.Elapsed.TotalMilliseconds);
    }
    samples.Sort();
    return samples[samples.Count / 2]; // median
}
