using NSubstitute;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Application.Orders.Queries.GetOrders;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class GetOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToReadServiceWithSameParameters()
    {
        var readService = Substitute.For<IOrderReadService>();
        var parameters = new OrderQueryParameters { Page = 2 };
        var page = new PagedResult<OrderListItemDto>(Array.Empty<OrderListItemDto>(), 2, 25, 0);
        readService.ListOrdersAsync(parameters, Arg.Any<CancellationToken>()).Returns(page);
        var handler = new GetOrdersQueryHandler(readService);

        var result = await handler.Handle(new GetOrdersQuery(parameters), CancellationToken.None);

        Assert.Same(page, result);
    }
}
