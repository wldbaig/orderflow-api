using OrderFlow.Domain.Common;
using OrderFlow.Domain.Entities;
using Xunit;

namespace OrderFlow.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidInput_TrimsValuesAndIsActive()
    {
        var product = Product.Create(" SKU-1 ", " Widget ", " Hardware ", 9.99m);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("Widget", product.Name);
        Assert.Equal("Hardware", product.Category);
        Assert.Equal(9.99m, product.UnitPrice);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_WithNullCategory_UsesEmptyCategory()
    {
        var product = Product.Create("SKU-1", "Widget", null!, 1m);

        Assert.Equal(string.Empty, product.Category);
    }

    [Fact]
    public void Create_WithBlankSku_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => Product.Create(" ", "Widget", "Hardware", 1m));

        Assert.Equal("Product SKU is required.", exception.Message);
    }

    [Fact]
    public void Create_WithBlankName_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => Product.Create("SKU-1", "", "Hardware", 1m));

        Assert.Equal("Product name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => Product.Create("SKU-1", "Widget", "Hardware", -1m));

        Assert.Equal("Product unit price cannot be negative.", exception.Message);
    }

    [Fact]
    public void Deactivate_MarksProductInactive()
    {
        var product = Product.Create("SKU-1", "Widget", "Hardware", 1m);

        product.Deactivate();

        Assert.False(product.IsActive);
    }
}
