using OrderFlow.Domain.Common;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Domain;

public sealed class OrderTests
{
    private static readonly (string Sku, string Name, int Quantity, decimal UnitPrice)[] SingleLine = { ("SKU-1", "Widget", 1, 1m) };

    [Fact]
    public void Create_WithValidInput_StartsPendingWithTrimmedValues()
    {
        var before = DateTime.UtcNow;

        var order = Order.Create(" ORD-2026-000001 ", " Acme Ltd ", " PO-1 ", " Leave at dock ", SingleLine);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("ORD-2026-000001", order.OrderNumber);
        Assert.Equal("Acme Ltd", order.CustomerName);
        Assert.Equal("PO-1", order.CustomerReference);
        Assert.Equal("Leave at dock", order.SpecialInstructions);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(OrderPriority.Medium, order.Priority);
        Assert.Equal(AiAnalysisStatus.Pending, order.AiAnalysisStatus);
        Assert.InRange(order.CreatedAtUtc, before, DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithNullOptionalFields_UsesNullReferenceAndEmptyInstructions()
    {
        var order = Order.Create("ORD-1", "Acme", null, null, SingleLine);

        Assert.Null(order.CustomerReference);
        Assert.Equal(string.Empty, order.SpecialInstructions);
    }

    [Fact]
    public void Create_WithSeveralLines_ComputesTotalsInTheAggregate()
    {
        var order = OrderFactory.CreateWithLines(("SKU-1", "Widget", 2, 10.50m), ("SKU-2", "Bolt", 3, 4m));

        Assert.Equal(33m, order.TotalAmount);
        Assert.Equal(5, order.TotalUnits);
        Assert.Equal(2, order.Lines.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankOrderNumber_Throws(string orderNumber)
    {
        var exception = Assert.Throws<DomainException>(() => Order.Create(orderNumber, "Acme", null, null, SingleLine));

        Assert.Equal("Order number is required.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCustomerName_Throws(string customerName)
    {
        var exception = Assert.Throws<DomainException>(() => Order.Create("ORD-1", customerName, null, null, SingleLine));

        Assert.Equal("Customer name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithoutLines_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => Order.Create("ORD-1", "Acme", null, null, Array.Empty<(string Sku, string Name, int Quantity, decimal UnitPrice)>()));

        Assert.Equal("An order must have at least one line.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_WithNonPositiveQuantity_Throws(int quantity)
    {
        var exception = Assert.Throws<DomainException>(() => OrderFactory.Create(quantity: quantity));

        Assert.Equal("Order line quantity must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => OrderFactory.Create(unitPrice: -0.01m));

        Assert.Equal("Order line unit price cannot be negative.", exception.Message);
    }

    [Fact]
    public void Create_WithZeroUnitPrice_IsAllowed()
    {
        var order = OrderFactory.Create(unitPrice: 0m);

        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void Create_WithBlankSku_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => OrderFactory.CreateWithLines((" ", "Widget", 1, 1m)));

        Assert.Equal("Order line requires a product SKU.", exception.Message);
    }

    [Fact]
    public void Create_WithBlankProductName_FallsBackToSku()
    {
        var order = OrderFactory.CreateWithLines((" SKU-9 ", "  ", 1, 1m));

        var line = Assert.Single(order.Lines);
        Assert.Equal("SKU-9", line.ProductSku);
        Assert.Equal("SKU-9", line.ProductName);
    }

    [Fact]
    public void LineTotal_IsQuantityTimesUnitPrice()
    {
        var order = OrderFactory.Create(quantity: 3, unitPrice: 7m);

        Assert.Equal(21m, Assert.Single(order.Lines).LineTotal);
    }

    [Fact]
    public void Lines_AreExposedReadOnly()
    {
        var order = OrderFactory.Create();

        Assert.True(((ICollection<OrderLine>)order.Lines).IsReadOnly);
    }

    [Fact]
    public void AddLine_WhilePending_RecalculatesTotalsAndTouchesOrder()
    {
        var order = OrderFactory.Create(quantity: 1, unitPrice: 5m);

        order.AddLine("SKU-2", "Bolt", 4, 2.5m);

        Assert.Equal(15m, order.TotalAmount);
        Assert.Equal(5, order.TotalUnits);
        Assert.NotNull(order.UpdatedAtUtc);
    }

    [Fact]
    public void AddLine_WhenNotPending_Throws()
    {
        var order = OrderFactory.Create();
        order.Confirm();

        var exception = Assert.Throws<DomainException>(() => order.AddLine("SKU-2", "Bolt", 1, 1m));

        Assert.Equal("Lines can only be added while the order is Pending.", exception.Message);
    }

    [Fact]
    public void Cancel_WhenPending_SetsCancelled()
    {
        var order = OrderFactory.Create();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.UpdatedAtUtc);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var order = OrderFactory.Create();
        order.Cancel();

        var exception = Assert.Throws<DomainException>(() => order.Cancel());

        Assert.Equal("Order is already cancelled.", exception.Message);
    }

    [Fact]
    public void Cancel_WhenConfirmed_Throws()
    {
        var order = OrderFactory.Create();
        order.Confirm();

        var exception = Assert.Throws<DomainException>(() => order.Cancel());

        Assert.Equal("Only Pending orders can be cancelled. This order is Confirmed.", exception.Message);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenPending_SetsConfirmed()
    {
        var order = OrderFactory.Create();

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenCancelled_Throws()
    {
        var order = OrderFactory.Create();
        order.Cancel();

        var exception = Assert.Throws<DomainException>(() => order.Confirm());

        Assert.Equal("Only Pending orders can be confirmed. This order is Cancelled.", exception.Message);
    }

    [Fact]
    public void ApplyIntelligence_StoresTrimmedSummaryPriorityAndNonBlankFlags()
    {
        var order = OrderFactory.Create();

        order.ApplyIntelligence("  Rush order  ", OrderPriority.High, new[] { " Tight deadline ", "", "  ", "COD" });

        Assert.Equal("Rush order", order.AiSummary);
        Assert.Equal(OrderPriority.High, order.Priority);
        Assert.Equal("Tight deadline\nCOD", order.AiRiskFlags);
        Assert.Equal(AiAnalysisStatus.Processed, order.AiAnalysisStatus);
        Assert.NotNull(order.AiAnalyzedAtUtc);
    }

    [Fact]
    public void ApplyIntelligence_WithNoFlags_StoresNullFlags()
    {
        var order = OrderFactory.Create();

        order.ApplyIntelligence("Routine", OrderPriority.Low, Array.Empty<string>());

        Assert.Null(order.AiRiskFlags);
        Assert.Equal(OrderPriority.Low, order.Priority);
    }

    [Fact]
    public void MarkIntelligenceFailed_SetsFailedButKeepsOrderValid()
    {
        var order = OrderFactory.Create();

        order.MarkIntelligenceFailed();

        Assert.Equal(AiAnalysisStatus.Failed, order.AiAnalysisStatus);
        Assert.NotNull(order.AiAnalyzedAtUtc);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(OrderPriority.Medium, order.Priority);
    }
}
