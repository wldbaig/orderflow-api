using NSubstitute;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Commands.CancelOrder;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class CancelOrderCommandHandlerTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _handler = new CancelOrderCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenOrderMissing_ThrowsNotFoundAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(new CancelOrderCommand(id), CancellationToken.None));

        _ = _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPending_CancelsAndSaves()
    {
        var order = OrderFactory.Create();
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, dto.Status);
        _ = _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyCancelled_ThrowsDomainExceptionAndDoesNotSave()
    {
        var order = OrderFactory.Create();
        order.Cancel();
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None));

        _ = _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
