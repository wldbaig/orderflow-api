using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.TestData;

public static class OrderFactory
{
    public static Order Create(string orderNumber = "ORD-2026-000001", string customerName = "Acme Ltd", string? specialInstructions = null, int quantity = 2, decimal unitPrice = 10m)
    {
        return Order.Create(orderNumber, customerName, "PO-1", specialInstructions, new[] { ("SKU-1", "Widget", quantity, unitPrice) });
    }

    public static Order CreateWithLines(params (string Sku, string Name, int Quantity, decimal UnitPrice)[] lines)
    {
        return Order.Create("ORD-2026-000001", "Acme Ltd", "PO-1", null, lines);
    }
}
