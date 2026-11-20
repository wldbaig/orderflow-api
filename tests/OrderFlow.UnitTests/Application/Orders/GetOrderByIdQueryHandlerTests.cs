using NSubstitute;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries.GetOrderById;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class GetOrderByIdQueryHandlerTests
{
    private readonly IOrderReadService _readService = Substitute.For<IOrderReadService>();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _handler = new GetOrderByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsReadModel()
    {
        var dto = new OrderDetailDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" };
        _readService.GetOrderDetailAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _handler.Handle(new GetOrderByIdQuery(dto.Id), CancellationToken.None);

        Assert.Same(dto, result);
    }

    [Fact]
    public async Task Handle_WhenMissing_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _readService.GetOrderDetailAsync(id, Arg.Any<CancellationToken>()).Returns((OrderDetailDto?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(new GetOrderByIdQuery(id), CancellationToken.None));

        Assert.Contains(id.ToString(), exception.Message);
    }
}
