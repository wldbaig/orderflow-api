using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.UnitTests.TestData;

public static class DbContextFactory
{
    public static OrderFlowDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<OrderFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new OrderFlowDbContext(options);
    }
}
