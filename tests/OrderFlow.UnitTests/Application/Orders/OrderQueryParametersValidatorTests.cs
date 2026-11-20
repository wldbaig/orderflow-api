using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class OrderQueryParametersValidatorTests
{
    private readonly OrderQueryParametersValidator _validator = new OrderQueryParametersValidator();

    [Fact]
    public void Validate_WithDefaults_Passes()
    {
        Assert.True(_validator.Validate(new OrderQueryParameters()).IsValid);
    }

    [Fact]
    public void Validate_WithConsistentRanges_Passes()
    {
        var parameters = new OrderQueryParameters
        {
            MinTotalUnits = 1,
            MaxTotalUnits = 10,
            MinTotalAmount = 5m,
            MaxTotalAmount = 50m,
            CreatedAfterUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBeforeUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.True(_validator.Validate(parameters).IsValid);
    }

    [Fact]
    public void Validate_WithMinUnitsAboveMax_Fails()
    {
        var result = _validator.Validate(new OrderQueryParameters { MinTotalUnits = 10, MaxTotalUnits = 5 });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "MinTotalUnits cannot be greater than MaxTotalUnits.");
    }

    [Fact]
    public void Validate_WithMinAmountAboveMax_Fails()
    {
        var result = _validator.Validate(new OrderQueryParameters { MinTotalAmount = 100m, MaxTotalAmount = 1m });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "MinTotalAmount cannot be greater than MaxTotalAmount.");
    }

    [Fact]
    public void Validate_WithCreatedAfterLaterThanBefore_Fails()
    {
        var parameters = new OrderQueryParameters
        {
            CreatedAfterUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBeforeUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.Validate(parameters);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "CreatedAfterUtc cannot be later than CreatedBeforeUtc.");
    }

    [Fact]
    public void Validate_WithNegativeThresholds_Fails()
    {
        var result = _validator.Validate(new OrderQueryParameters { MinTotalUnits = -1, MaxTotalUnits = -1, MinTotalAmount = -1m, MaxTotalAmount = -1m });

        Assert.Contains(result.Errors, e => e.PropertyName == "MinTotalUnits");
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxTotalUnits");
        Assert.Contains(result.Errors, e => e.PropertyName == "MinTotalAmount");
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxTotalAmount");
    }

    [Fact]
    public void Validate_WithUndefinedEnums_Fails()
    {
        var result = _validator.Validate(new OrderQueryParameters { Status = (OrderStatus)99, Priority = (OrderPriority)99, SortBy = (OrderSortField)99 });

        Assert.Contains(result.Errors, e => e.PropertyName == "Status");
        Assert.Contains(result.Errors, e => e.PropertyName == "Priority");
        Assert.Contains(result.Errors, e => e.PropertyName == "SortBy");
    }

    [Fact]
    public void Validate_WithTooLongTextFilters_Fails()
    {
        var result = _validator.Validate(new OrderQueryParameters { CustomerName = new string('a', 201), RiskFlagContains = new string('b', 201) });

        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerName");
        Assert.Contains(result.Errors, e => e.PropertyName == "RiskFlagContains");
    }
}
