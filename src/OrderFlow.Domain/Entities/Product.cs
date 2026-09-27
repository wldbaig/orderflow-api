using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// Reference/master data: a product customers can order. Read-heavy and rarely changed,
/// which makes the catalog a natural fit for response caching with write invalidation.
/// </summary>
public class Product
{
    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
        Category = string.Empty;
    }

    private Product(string sku, string name, string category, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        Sku = sku;
        Name = name;
        Category = category;
        UnitPrice = unitPrice;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Product Create(string sku, string name, string category, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("Product SKU is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (unitPrice < 0) throw new DomainException("Product unit price cannot be negative.");

        return new Product(sku.Trim(), name.Trim(), (category ?? string.Empty).Trim(), unitPrice);
    }

    public void Deactivate() => IsActive = false;
}
