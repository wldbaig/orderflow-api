using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Infrastructure.Persistence;

public sealed class OrderReadServiceTests : IDisposable
{
    private readonly OrderFlowDbContext _db = DbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly OrderReadService _service;

    public OrderReadServiceTests()
    {
        var small = OrderFactory.Create(orderNumber: "ORD-1", customerName: "Acme Ltd", quantity: 5, unitPrice: 10m);
        var large = OrderFactory.Create(orderNumber: "ORD-2", customerName: "Globex", quantity: 50, unitPrice: 20m);
        large.ApplyIntelligence("Big", OrderPriority.High, new[] { "Requests cash on delivery" });
        var cancelled = OrderFactory.Create(orderNumber: "ORD-3", customerName: "Acme Industries", quantity: 1, unitPrice: 500m);
        cancelled.Cancel();
        _db.Orders.AddRange(small, large, cancelled);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        _service = new OrderReadService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<string[]> NumbersAsync(OrderQueryParameters parameters)
    {
        var page = await _service.ListOrdersAsync(parameters, CancellationToken.None);
        return page.Items.Select(i => i.OrderNumber).ToArray();
    }

    [Fact]
    public async Task List_WithoutFilters_ReturnsAllOrders()
    {
        var page = await _service.ListOrdersAsync(new OrderQueryParameters(), CancellationToken.None);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        Assert.Equal(new[] { "ORD-3" }, await NumbersAsync(new OrderQueryParameters { Status = OrderStatus.Cancelled }));
    }

    [Fact]
    public async Task List_FiltersByPriority()
    {
        Assert.Equal(new[] { "ORD-2" }, await NumbersAsync(new OrderQueryParameters { Priority = OrderPriority.High }));
    }

    [Fact]
    public async Task List_FiltersByCustomerNameSubstring()
    {
        var numbers = await NumbersAsync(new OrderQueryParameters { CustomerName = "Acme", SortBy = OrderSortField.OrderNumber, SortDescending = false });

        Assert.Equal(new[] { "ORD-1", "ORD-3" }, numbers);
    }

    [Fact]
    public async Task List_FiltersByUnitRange()
    {
        var numbers = await NumbersAsync(new OrderQueryParameters { MinTotalUnits = 5, MaxTotalUnits = 10 });

        Assert.Equal(new[] { "ORD-1" }, numbers);
    }

    [Fact]
    public async Task List_FiltersByAmountRange()
    {
        var numbers = await NumbersAsync(new OrderQueryParameters { MinTotalAmount = 100m, MaxTotalAmount = 600m });

        Assert.Equal(new[] { "ORD-3" }, numbers);
    }

    [Fact]
    public async Task List_FiltersByRiskFlag()
    {
        Assert.Equal(new[] { "ORD-2" }, await NumbersAsync(new OrderQueryParameters { RiskFlagContains = "cash" }));
    }

    [Fact]
    public async Task List_FiltersByCreatedWindow()
    {
        var future = await NumbersAsync(new OrderQueryParameters { CreatedAfterUtc = DateTime.UtcNow.AddDays(1) });
        var past = await NumbersAsync(new OrderQueryParameters { CreatedBeforeUtc = DateTime.UtcNow.AddDays(-1) });

        Assert.Empty(future);
        Assert.Empty(past);
    }

    [Fact]
    public async Task List_SortsByAmountAscending()
    {
        var numbers = await NumbersAsync(new OrderQueryParameters { SortBy = OrderSortField.TotalAmount, SortDescending = false });

        Assert.Equal(new[] { "ORD-1", "ORD-3", "ORD-2" }, numbers);
    }

    [Fact]
    public async Task List_PagesResults()
    {
        var page = await _service.ListOrdersAsync(new OrderQueryParameters { Page = 2, PageSize = 2, SortBy = OrderSortField.OrderNumber, SortDescending = false }, CancellationToken.None);

        Assert.Equal(new[] { "ORD-3" }, page.Items.Select(i => i.OrderNumber));
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task List_ProjectsListFields()
    {
        var page = await _service.ListOrdersAsync(new OrderQueryParameters { Priority = OrderPriority.High }, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal("Globex", item.CustomerName);
        Assert.Equal(1000m, item.TotalAmount);
        Assert.Equal(50, item.TotalUnits);
        Assert.Equal(1, item.LineCount);
        Assert.Equal(AiAnalysisStatus.Processed, item.AiAnalysisStatus);
        Assert.Equal("Big", item.AiSummary);
    }

    [Fact]
    public async Task GetDetail_WhenFound_ProjectsOrderLinesAndFlags()
    {
        var id = _db.Orders.Single(o => o.OrderNumber == "ORD-2").Id;

        var dto = await _service.GetOrderDetailAsync(id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("ORD-2", dto.OrderNumber);
        Assert.Equal(new[] { "Requests cash on delivery" }, dto.AiRiskFlags);
        var line = Assert.Single(dto.Lines);
        Assert.Equal(1000m, line.LineTotal);
    }

    [Fact]
    public async Task GetDetail_WhenMissing_ReturnsNull()
    {
        Assert.Null(await _service.GetOrderDetailAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
