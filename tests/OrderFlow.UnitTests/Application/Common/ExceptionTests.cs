using FluentValidation.Results;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Intelligence;
using OrderFlow.Domain.Enums;
using Xunit;

namespace OrderFlow.UnitTests.Application.Common;

public sealed class ExceptionTests
{
    [Fact]
    public void ValidationException_GroupsFailuresByProperty()
    {
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Name", "Required"),
            new ValidationFailure("Name", "Too long"),
            new ValidationFailure("Age", "Invalid")
        });

        Assert.Equal("One or more validation errors occurred.", exception.Message);
        Assert.Equal(new[] { "Required", "Too long" }, exception.Errors["Name"]);
        Assert.Equal(new[] { "Invalid" }, exception.Errors["Age"]);
    }

    [Fact]
    public void NotFoundException_NamesEntityAndKey()
    {
        var exception = new NotFoundException("Order", 42);

        Assert.Equal("Order with key '42' was not found.", exception.Message);
    }

    [Fact]
    public void OrderIntelligenceResult_Failed_IsUnsuccessfulWithMediumPriority()
    {
        var result = OrderIntelligenceResult.Failed();

        Assert.False(result.Succeeded);
        Assert.Equal(string.Empty, result.Summary);
        Assert.Equal(OrderPriority.Medium, result.Priority);
        Assert.Empty(result.RiskFlags);
    }
}
