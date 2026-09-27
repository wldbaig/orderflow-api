using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderFlow.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c>. It does not depend on the API's
/// configuration pipeline; the connection string comes from the <c>ORDERFLOW_CONNECTION</c>
/// environment variable, falling back to LocalDB so migrations work out of the box.
/// </summary>
public sealed class OrderFlowDbContextFactory : IDesignTimeDbContextFactory<OrderFlowDbContext>
{
    public OrderFlowDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ORDERFLOW_CONNECTION")
            ?? "Server=localhost\\SQLEXPRESS;Database=OrderFlow;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<OrderFlowDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsAssembly(typeof(OrderFlowDbContext).Assembly.FullName))
            .Options;

        return new OrderFlowDbContext(options);
    }
}
