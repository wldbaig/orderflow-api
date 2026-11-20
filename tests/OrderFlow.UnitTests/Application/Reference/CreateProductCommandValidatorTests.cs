using OrderFlow.Application.Reference.Commands.CreateProduct;
using Xunit;

namespace OrderFlow.UnitTests.Application.Reference;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new CreateProductCommandValidator();

    private static CreateProductCommand ValidCommand() => new CreateProductCommand("SKU-1", "Widget", "Hardware", 1m);

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_WithEmptySkuAndName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Sku = "", Name = "" });

        Assert.Contains(result.Errors, e => e.PropertyName == "Sku");
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithTooLongFields_Fails()
    {
        var result = _validator.Validate(new CreateProductCommand(new string('s', 51), new string('n', 201), new string('c', 101), 1m));

        Assert.Contains(result.Errors, e => e.PropertyName == "Sku");
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Category");
    }

    [Fact]
    public void Validate_WithNegativePrice_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { UnitPrice = -1m });

        Assert.Contains(result.Errors, e => e.PropertyName == "UnitPrice");
    }
}
