using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// A single line on an order. This is part of the <see cref="Order"/> aggregate and
/// can only be created through <see cref="Order.AddLine"/>, so its invariants are
/// always enforced by the aggregate root.
/// </summary>
public class OrderLine
{
    // EF Core materialisation constructor.
    private OrderLine()
    {
        ProductSku = string.Empty;
        ProductName = string.Empty;
    }

    internal OrderLine(string productSku, string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productSku))
            throw new DomainException("Order line requires a product SKU.");
        if (quantity <= 0)
            throw new DomainException("Order line quantity must be greater than zero.");
        if (unitPrice < 0)
            throw new DomainException("Order line unit price cannot be negative.");

        Id = Guid.NewGuid();
        ProductSku = productSku.Trim();
        ProductName = string.IsNullOrWhiteSpace(productName) ? productSku.Trim() : productName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }

    public string ProductSku { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    /// <summary>Line total, computed in the domain — never in a controller or query.</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
