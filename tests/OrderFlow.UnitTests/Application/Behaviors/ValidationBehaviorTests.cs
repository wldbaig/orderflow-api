using FluentValidation;
using MediatR;
using OrderFlow.Application.Common.Behaviors;
using OrderFlow.UnitTests.TestData;
using Xunit;
using ValidationException = OrderFlow.Application.Common.Exceptions.ValidationException;

namespace OrderFlow.UnitTests.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    private int _nextCalls;

    private RequestHandlerDelegate<string> Next()
    {
        return () =>
        {
            _nextCalls++;
            return Task.FromResult("handled");
        };
    }

    private static IValidator<SampleRequest> NameRequiredValidator()
    {
        var validator = new InlineValidator<SampleRequest>();
        validator.RuleFor(r => r.Name).NotEmpty();
        return validator;
    }

    [Fact]
    public async Task Handle_WithoutValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());

        var response = await behavior.Handle(new SampleRequest(""), Next(), CancellationToken.None);

        Assert.Equal("handled", response);
        Assert.Equal(1, _nextCalls);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { NameRequiredValidator() });

        var response = await behavior.Handle(new SampleRequest("ok"), Next(), CancellationToken.None);

        Assert.Equal("handled", response);
        Assert.Equal(1, _nextCalls);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsAndShortCircuits()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { NameRequiredValidator() });

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(new SampleRequest(""), Next(), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("Name"));
        Assert.Equal(0, _nextCalls);
    }
}
