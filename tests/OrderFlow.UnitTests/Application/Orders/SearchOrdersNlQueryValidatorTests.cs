using OrderFlow.Application.Orders.Queries.SearchOrdersNl;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class SearchOrdersNlQueryValidatorTests
{
    private readonly SearchOrdersNlQueryValidator _validator = new SearchOrdersNlQueryValidator();

    [Fact]
    public void Validate_WithPhrase_Passes()
    {
        Assert.True(_validator.Validate(new SearchOrdersNlQuery("high priority orders", null, null)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankPhrase_Fails(string phrase)
    {
        var result = _validator.Validate(new SearchOrdersNlQuery(phrase, null, null));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "A search phrase is required.");
    }

    [Fact]
    public void Validate_WithTooLongPhrase_Fails()
    {
        var result = _validator.Validate(new SearchOrdersNlQuery(new string('a', 501), null, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "Phrase");
    }
}
