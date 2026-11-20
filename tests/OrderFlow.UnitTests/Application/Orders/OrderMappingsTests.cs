using OrderFlow.Application.Orders.Mapping;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class OrderMappingsTests
{
    [Fact]
    public void ToDetailDto_MapsEveryOrderAndLineField()
    {
        var order = OrderFactory.CreateWithLines(("SKU-1", "Widget", 2, 10m), ("SKU-2", "Bolt", 1, 3m));
        order.ApplyIntelligence("Summary", OrderPriority.High, new[] { "COD", "Rush" });

        var dto = order.ToDetailDto();

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(order.OrderNumber, dto.OrderNumber);
        Assert.Equal(order.CustomerName, dto.CustomerName);
        Assert.Equal(order.CustomerReference, dto.CustomerReference);
        Assert.Equal(order.SpecialInstructions, dto.SpecialInstructions);
        Assert.Equal(order.Status, dto.Status);
        Assert.Equal(OrderPriority.High, dto.Priority);
        Assert.Equal(23m, dto.TotalAmount);
        Assert.Equal(3, dto.TotalUnits);
        Assert.Equal(order.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(order.UpdatedAtUtc, dto.UpdatedAtUtc);
        Assert.Equal(AiAnalysisStatus.Processed, dto.AiAnalysisStatus);
        Assert.Equal("Summary", dto.AiSummary);
        Assert.Equal(new[] { "COD", "Rush" }, dto.AiRiskFlags);
        Assert.Equal(order.AiAnalyzedAtUtc, dto.AiAnalyzedAtUtc);
        Assert.Collection(
            dto.Lines,
            line =>
            {
                Assert.Equal("SKU-1", line.ProductSku);
                Assert.Equal("Widget", line.ProductName);
                Assert.Equal(2, line.Quantity);
                Assert.Equal(10m, line.UnitPrice);
                Assert.Equal(20m, line.LineTotal);
            },
            line => Assert.Equal(3m, line.LineTotal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SplitFlags_WithNoContent_ReturnsEmpty(string? flags)
    {
        Assert.Empty(OrderMappings.SplitFlags(flags));
    }

    [Fact]
    public void SplitFlags_TrimsAndDropsEmptyEntries()
    {
        Assert.Equal(new[] { "COD", "Rush" }, OrderMappings.SplitFlags(" COD \n\n Rush \n"));
    }
}
