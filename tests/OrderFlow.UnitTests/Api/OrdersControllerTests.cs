using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OrderFlow.Api.Common;
using OrderFlow.Api.Contracts;
using OrderFlow.Api.Controllers;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Commands.CancelOrder;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Application.Orders.Queries.GetOrderById;
using OrderFlow.Application.Orders.Queries.GetOrders;
using OrderFlow.Application.Orders.Queries.SearchOrdersNl;
using Xunit;

namespace OrderFlow.UnitTests.Api;

public sealed class OrdersControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _controller = new OrdersController(_sender);
    }

    [Fact]
    public async Task Create_MapsRequestToCommandAndReturns201()
    {
        var dto = new OrderDetailDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" };
        _sender.Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>()).Returns(dto);
        var request = new CreateOrderRequest("Acme", "PO-1", "ASAP", new List<CreateOrderLineRequest> { new CreateOrderLineRequest("SKU-1", "Widget", 2, 5m) });

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(OrdersController.GetById), created.ActionName);
        Assert.Equal(dto.Id, created.RouteValues!["id"]);
        var payload = Assert.IsType<ApiResponse<OrderDetailDto>>(created.Value);
        Assert.True(payload.Success);
        Assert.Same(dto, payload.Data);
        _ = _sender.Received(1).Send(
            Arg.Is<CreateOrderCommand>(c => c.CustomerName == "Acme" && c.CustomerReference == "PO-1" && c.SpecialInstructions == "ASAP" && c.Lines.Count == 1 && c.Lines[0].ProductSku == "SKU-1" && c.Lines[0].Quantity == 2 && c.Lines[0].UnitPrice == 5m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithNullLines_SendsEmptyLineList()
    {
        _sender.Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>()).Returns(new OrderDetailDto());

        await _controller.Create(new CreateOrderRequest("Acme", null, null, null!), CancellationToken.None);

        _ = _sender.Received(1).Send(Arg.Is<CreateOrderCommand>(c => c.Lines.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_ReturnsPageInEnvelope()
    {
        var page = new PagedResult<OrderListItemDto>(Array.Empty<OrderListItemDto>(), 1, 25, 0);
        _sender.Send(Arg.Any<GetOrdersQuery>(), Arg.Any<CancellationToken>()).Returns(page);

        var result = await _controller.List(new OrderQueryParameters(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, Assert.IsType<ApiResponse<PagedResult<OrderListItemDto>>>(ok.Value).Data);
    }

    [Fact]
    public async Task GetById_ReturnsOrderInEnvelope()
    {
        var id = Guid.NewGuid();
        var dto = new OrderDetailDto { Id = id };
        _sender.Send(Arg.Is<GetOrderByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, Assert.IsType<ApiResponse<OrderDetailDto>>(ok.Value).Data);
    }

    [Fact]
    public async Task Cancel_ReturnsOrderWithMessage()
    {
        var id = Guid.NewGuid();
        _sender.Send(Arg.Is<CancelOrderCommand>(c => c.OrderId == id), Arg.Any<CancellationToken>()).Returns(new OrderDetailDto { Id = id });

        var result = await _controller.Cancel(id, CancellationToken.None);

        var payload = Assert.IsType<ApiResponse<OrderDetailDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Order cancelled.", payload.Message);
        Assert.Equal(id, payload.Data!.Id);
    }

    [Fact]
    public async Task SearchNaturalLanguage_ForwardsPhraseAndPaging()
    {
        var page = new PagedResult<OrderListItemDto>(Array.Empty<OrderListItemDto>(), 2, 10, 0);
        var searchResult = new NlSearchResult("late orders", null, new OrderQueryParameters(), page);
        _sender.Send(Arg.Any<SearchOrdersNlQuery>(), Arg.Any<CancellationToken>()).Returns(searchResult);

        var result = await _controller.SearchNaturalLanguage(new NlSearchRequest("late orders", 2, 10), CancellationToken.None);

        Assert.Same(searchResult, Assert.IsType<ApiResponse<NlSearchResult>>(Assert.IsType<OkObjectResult>(result).Value).Data);
        _ = _sender.Received(1).Send(Arg.Is<SearchOrdersNlQuery>(q => q.Phrase == "late orders" && q.Page == 2 && q.PageSize == 10), Arg.Any<CancellationToken>());
    }
}
