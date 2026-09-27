using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common;
using OrderFlow.Api.Contracts;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Commands.CancelOrder;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Application.Orders.Queries.GetOrderById;
using OrderFlow.Application.Orders.Queries.GetOrders;
using OrderFlow.Application.Orders.Queries.SearchOrdersNl;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// Thin controller: it maps the HTTP edge to MediatR requests and shapes the response.
/// No business logic, no data access, no validation lives here.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Places a new order. The free-text instructions are analysed by AI during intake.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var command = new CreateOrderCommand(
            request.CustomerName,
            request.CustomerReference,
            request.SpecialInstructions,
            (request.Lines ?? new())
                .Select(l => new CreateOrderLineInput(l.ProductSku, l.ProductName, l.Quantity, l.UnitPrice))
                .ToList());

        var order = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ApiResponse<OrderDetailDto>.Ok(order));
    }

    /// <summary>Paged, filtered and sorted list of orders (projected straight to a DTO).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] OrderQueryParameters parameters, CancellationToken ct)
    {
        var result = await _sender.Send(new GetOrdersQuery(parameters), ct);
        return Ok(ApiResponse<PagedResult<OrderListItemDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), ct);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order));
    }

    /// <summary>Cancels an order (allowed only while Pending — enforced in the domain).</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new CancelOrderCommand(id), ct);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order cancelled."));
    }

    /// <summary>
    /// Natural-language search. The AI maps the phrase to structured, validated filters; the
    /// SAME safe query as <see cref="List"/> then runs. The model never touches the database.
    /// </summary>
    [HttpPost("search-nl")]
    [ProducesResponseType(typeof(ApiResponse<NlSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SearchNaturalLanguage([FromBody] NlSearchRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SearchOrdersNlQuery(request.Phrase, request.Page, request.PageSize), ct);
        return Ok(ApiResponse<NlSearchResult>.Ok(result));
    }
}
