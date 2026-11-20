using OrderFlow.Application.Orders.Queries;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class OrderQueryParametersTests
{
    [Fact]
    public void Defaults_AreFirstPageOf25NewestFirst()
    {
        var parameters = new OrderQueryParameters();

        Assert.Equal(1, parameters.Page);
        Assert.Equal(25, parameters.PageSize);
        Assert.Equal(OrderSortField.CreatedAtUtc, parameters.SortBy);
        Assert.True(parameters.SortDescending);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(7, 7)]
    public void Page_BelowOne_IsClampedToOne(int requested, int expected)
    {
        var parameters = new OrderQueryParameters { Page = requested };

        Assert.Equal(expected, parameters.Page);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 50)]
    [InlineData(200, 200)]
    [InlineData(5000, OrderQueryParameters.MaxPageSize)]
    public void PageSize_IsClampedBetweenOneAndMax(int requested, int expected)
    {
        var parameters = new OrderQueryParameters { PageSize = requested };

        Assert.Equal(expected, parameters.PageSize);
    }
}
