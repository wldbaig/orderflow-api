using OrderFlow.Application.Orders.Commands.CreateOrder;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new CreateOrderCommandValidator();

    private static CreateOrderCommand ValidCommand() =>
        new CreateOrderCommand("Acme", "PO-1", "Deliver to dock 3", new[] { new CreateOrderLineInput("SKU-1", "Widget", 1, 1m) });

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyCustomerName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CustomerName = "" });

        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerName" && e.ErrorMessage == "Customer name is required.");
    }

    [Fact]
    public void Validate_WithTooLongCustomerName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CustomerName = new string('a', 201) });

        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void Validate_WithTooLongCustomerReference_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CustomerReference = new string('a', 101) });

        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerReference");
    }

    [Fact]
    public void Validate_WithTooLongSpecialInstructions_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SpecialInstructions = new string('a', 4001) });

        Assert.Contains(result.Errors, e => e.PropertyName == "SpecialInstructions");
    }

    [Fact]
    public void Validate_WithNoLines_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Lines = Array.Empty<CreateOrderLineInput>() });

        Assert.Contains(result.Errors, e => e.PropertyName == "Lines" && e.ErrorMessage == "An order must have at least one line.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveQuantity_Fails(int quantity)
    {
        var result = _validator.Validate(ValidCommand() with { Lines = new[] { new CreateOrderLineInput("SKU-1", "Widget", quantity, 1m) } });

        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("Quantity") && e.ErrorMessage == "Quantity must be greater than zero.");
    }

    [Fact]
    public void Validate_WithNegativeUnitPrice_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Lines = new[] { new CreateOrderLineInput("SKU-1", "Widget", 1, -1m) } });

        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("UnitPrice") && e.ErrorMessage == "Unit price cannot be negative.");
    }

    [Fact]
    public void Validate_WithEmptySku_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Lines = new[] { new CreateOrderLineInput("", "Widget", 1, 1m) } });

        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("ProductSku"));
    }

    [Fact]
    public void Validate_WithTooLongSku_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Lines = new[] { new CreateOrderLineInput(new string('S', 51), "Widget", 1, 1m) } });

        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("ProductSku"));
    }

    [Fact]
    public void Validate_WithTooLongProductName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Lines = new[] { new CreateOrderLineInput("SKU-1", new string('n', 201), 1, 1m) } });

        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("ProductName"));
    }
}
