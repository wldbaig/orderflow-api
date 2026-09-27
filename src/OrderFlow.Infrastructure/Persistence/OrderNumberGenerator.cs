using System.Data;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Persistence;

/// <summary>
/// Generates order numbers like <c>ORD-2026-000123</c> using a SQL sequence, so numbers are
/// unique and monotonic without a table scan or a race on <c>COUNT(*)</c>.
/// </summary>
public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly OrderFlowDbContext _db;

    public OrderNumberGenerator(OrderFlowDbContext db) => _db = db;

    public async Task<string> NextAsync(CancellationToken ct)
    {
        // Executed as a plain scalar command: "NEXT VALUE FOR" cannot appear inside the
        // subquery that SqlQueryRaw would otherwise compose it into.
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT NEXT VALUE FOR {OrderFlowDbContext.OrderNumberSequence}";
        var result = await command.ExecuteScalarAsync(ct);
        var next = Convert.ToInt64(result);

        return $"ORD-{DateTime.UtcNow:yyyy}-{next:000000}";
    }
}
