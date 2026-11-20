using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application;
using OrderFlow.Application.Common.Behaviors;
using OrderFlow.Application.Orders.Commands.CancelOrder;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Application.Orders.Dtos;
using Xunit;

namespace OrderFlow.UnitTests.Composition;

public sealed class ApplicationDependencyInjectionTests
{
    private readonly IServiceCollection _services = new ServiceCollection().AddApplication();

    [Fact]
    public void AddApplication_RegistersHandlersByAssemblyScan()
    {
        Assert.Contains(_services, d => d.ServiceType == typeof(IRequestHandler<CancelOrderCommand, OrderDetailDto>));
        Assert.Contains(_services, d => d.ServiceType == typeof(IRequestHandler<CreateOrderCommand, OrderDetailDto>));
    }

    [Fact]
    public void AddApplication_RegistersValidatorsByAssemblyScan()
    {
        Assert.Contains(_services, d => d.ServiceType == typeof(IValidator<CreateOrderCommand>));
    }

    [Fact]
    public void AddApplication_RegistersLoggingBehaviorBeforeValidationBehavior()
    {
        var behaviors = _services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();

        var logging = behaviors.IndexOf(typeof(LoggingBehavior<,>));
        var validation = behaviors.IndexOf(typeof(ValidationBehavior<,>));
        Assert.True(logging >= 0 && validation > logging);
    }
}
