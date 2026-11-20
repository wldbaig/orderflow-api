using OrderFlow.Application.Orders.Queries;
using OrderFlow.Application.Orders.Queries.GetOrders;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class GetOrdersQueryValidatorTests
{
    private readonly GetOrdersQueryValidator _validator = new GetOrdersQueryValidator();

    [Fact]
    public void Validate_WithValidParameters_Passes()
    {
        Assert.True(_validator.Validate(new GetOrdersQuery(new OrderQueryParameters())).IsValid);
    }

    [Fact]
    public void Validate_WithInvalidParameters_FailsThroughNestedValidator()
    {
        var result = _validator.Validate(new GetOrdersQuery(new OrderQueryParameters { MinTotalUnits = -1 }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithNullParameters_Fails()
    {
        var result = _validator.Validate(new GetOrdersQuery(null!));

        Assert.Contains(result.Errors, e => e.PropertyName == "Parameters");
    }
}
