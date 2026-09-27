using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a realistic dataset so performance work is measured against real volume, not a toy.
/// Products are seeded once; orders are generated in batches (with varied dates, statuses,
/// instructions and AI results) so filtering/sorting/paging can be exercised meaningfully.
/// </summary>
public sealed class DataSeeder
{
    public const int DefaultOrderCount = 50_000;

    private readonly OrderFlowDbContext _db;
    private readonly ILogger<DataSeeder> _logger;
    private readonly Random _rng = new(20260926);

    public DataSeeder(OrderFlowDbContext db, ILogger<DataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(int orderCount = DefaultOrderCount, CancellationToken ct = default)
    {
        var products = await SeedProductsAsync(ct);

        var existing = await _db.Orders.CountAsync(ct);
        if (existing >= orderCount)
        {
            _logger.LogInformation("Order seed skipped; {Existing} orders already present.", existing);
            return;
        }

        var toCreate = orderCount - existing;
        _logger.LogInformation("Seeding {Count} orders...", toCreate);

        var autoDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            const int batchSize = 2_000;
            var seq = existing + 1;
            for (var created = 0; created < toCreate; created += batchSize)
            {
                var thisBatch = Math.Min(batchSize, toCreate - created);
                for (var i = 0; i < thisBatch; i++)
                {
                    var order = BuildOrder(seq++, products);
                    await _db.Orders.AddAsync(order, ct);
                }

                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
                _logger.LogInformation("  seeded {Done}/{Total} orders", created + thisBatch, toCreate);
            }
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = autoDetect;
        }

        // Keep the order-number sequence ahead of seeded numbers so real orders never collide.
        // Values are a compile-time constant name and a validated int — no user input in the DDL.
        var restartSql = "ALTER SEQUENCE " + OrderFlowDbContext.OrderNumberSequence
                         + " RESTART WITH " + (orderCount + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
        await _db.Database.ExecuteSqlRawAsync(restartSql, ct);

        _logger.LogInformation("Order seeding complete.");
    }

    private async Task<IReadOnlyList<(string Sku, string Name, decimal Price)>> SeedProductsAsync(CancellationToken ct)
    {
        var catalog = ProductCatalog();
        if (!await _db.Products.AnyAsync(ct))
        {
            foreach (var (sku, name, category, price) in catalog)
                await _db.Products.AddAsync(Product.Create(sku, name, category, price), ct);
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
            _logger.LogInformation("Seeded {Count} products.", catalog.Count);
        }

        return catalog.Select(c => (c.Sku, c.Name, c.Price)).ToList();
    }

    private Order BuildOrder(int seq, IReadOnlyList<(string Sku, string Name, decimal Price)> products)
    {
        var customer = Customers[_rng.Next(Customers.Length)];
        var (instructions, forcePriority) = Instructions[_rng.Next(Instructions.Length)];

        var lineCount = _rng.Next(1, 5);
        var lines = new List<(string, string, int, decimal)>();
        for (var i = 0; i < lineCount; i++)
        {
            var product = products[_rng.Next(products.Count)];
            // Occasionally seed a very large quantity so the "unusual quantity" flag appears.
            var qty = _rng.NextDouble() < 0.05 ? _rng.Next(1200, 3000) : _rng.Next(1, 60);
            lines.Add((product.Sku, product.Name, qty, product.Price));
        }

        var orderNumber = $"ORD-{DateTime.UtcNow:yyyy}-{seq:000000}";
        var order = Order.Create(orderNumber, customer, $"REF-{_rng.Next(10000, 99999)}", instructions, lines);

        // Vary creation date across the last ~180 days for realistic time filtering.
        var createdAt = DateTime.UtcNow.AddDays(-_rng.Next(0, 180)).AddMinutes(-_rng.Next(0, 1440));
        _db.Entry(order).Property(o => o.CreatedAtUtc).CurrentValue = createdAt;

        // ~8% of orders simulate an AI outage (saved with unprocessed AI fields); the rest get analysed.
        if (_rng.NextDouble() < 0.08)
        {
            order.MarkIntelligenceFailed();
        }
        else
        {
            var (summary, priority, flags) = Analyse(order, instructions, forcePriority);
            order.ApplyIntelligence(summary, priority, flags);
        }

        // Vary status: most Pending, some Confirmed/Shipped/Completed, a few Cancelled.
        var roll = _rng.NextDouble();
        if (roll is >= 0.55 and < 0.80) order.Confirm();
        else if (roll >= 0.80 && order.Status == OrderStatus.Pending) order.Cancel();

        return order;
    }

    private (string Summary, OrderPriority Priority, List<string> Flags) Analyse(Order order, string instructions, OrderPriority? forced)
    {
        var text = instructions.ToLowerInvariant();
        var flags = new List<string>();
        if (text.Contains("cash on delivery") || text.Contains("cod")) flags.Add("Requests cash on delivery");
        if (text.Contains("discount") || text.Contains("free")) flags.Add("Pricing concession requested");
        if (text.Contains("urgent") || text.Contains("asap") || text.Contains("same day")) flags.Add("Tight delivery deadline");
        if (order.TotalUnits > 1000) flags.Add($"Unusually large quantity ({order.TotalUnits} units)");

        var priority = forced ?? (flags.Count >= 2 ? OrderPriority.High : flags.Count == 1 ? OrderPriority.Medium : OrderPriority.Low);
        var summary = $"{order.TotalUnits} units across {order.Lines.Count} line(s) for {order.CustomerName}.";
        return (summary, priority, flags);
    }

    private static readonly string[] Customers =
    {
        "Northwind Traders", "Contoso Retail", "Fabrikam Wholesale", "Adventure Works", "Tailspin Toys",
        "Wingtip Distributors", "Litware Supplies", "Proseware Industrial", "Fourth Coffee", "Graphic Design Institute",
        "Alpine Ski House", "Blue Yonder Logistics", "City Power & Light", "Coho Vineyard", "Consolidated Messenger",
        "Humongous Insurance", "Lucerne Publishing", "Margie's Travel", "Nod Publishers", "School of Fine Art",
        "Southridge Video", "The Phone Company", "Trey Research", "VanArsdel Ltd", "Woodgrove Bank",
        "First Up Consultants", "Relecloud", "Best For You Organics", "The Landon Hotel", "Munson's Pickles"
    };

    // (instruction text, optional forced priority)
    private static readonly (string Text, OrderPriority? Priority)[] Instructions =
    {
        ("Please deliver to the loading dock before noon.", null),
        ("Standard delivery is fine, no rush on this one.", OrderPriority.Low),
        ("URGENT - needed same day, customer event tonight.", OrderPriority.High),
        ("Customer requests cash on delivery, please confirm driver can accept.", OrderPriority.High),
        ("Can we get a discount for this bulk order? Repeat customer.", null),
        ("Fragile items - handle with care, signature required.", null),
        ("Deliver to new address, not the one on file.", null),
        ("ASAP please, production line is waiting on these parts.", OrderPriority.High),
        ("No special instructions.", OrderPriority.Low),
        ("Please include a printed invoice with the shipment.", null),
        ("Split delivery across two dates if possible.", null),
        ("Gift wrap requested, do not include pricing on packing slip.", null),
        ("Call ahead 30 minutes before arrival.", null),
        ("Large recurring order - set up standing monthly delivery.", null),
        ("", OrderPriority.Low)
    };

    private static List<(string Sku, string Name, string Category, decimal Price)> ProductCatalog()
    {
        var list = new List<(string, string, string, decimal)>();
        var categories = new[] { "Fasteners", "Adhesives", "Packaging", "Safety", "Electrical", "Tools", "Cleaning", "Office" };
        var names = new[] { "Standard", "Heavy-Duty", "Industrial", "Premium", "Economy", "Pro", "Bulk", "Compact", "XL", "Mini" };
        var item = new[] { "Bolt", "Tape", "Box", "Gloves", "Cable", "Wrench", "Cleaner", "Binder", "Sealant", "Label" };
        var n = 1;
        foreach (var cat in categories)
            foreach (var name in names)
            {
                var i = item[(n - 1) % item.Length];
                list.Add(($"SKU-{n:0000}", $"{name} {i}", cat, Math.Round((decimal)(2 + (n * 1.37 % 95)), 2)));
                n++;
            }
        return list; // 80 products
    }
}
