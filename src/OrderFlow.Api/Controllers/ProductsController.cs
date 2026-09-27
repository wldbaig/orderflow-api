using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common;
using OrderFlow.Api.Contracts;
using OrderFlow.Application.Reference.Commands.CreateProduct;
using OrderFlow.Application.Reference.Dtos;
using OrderFlow.Application.Reference.Queries.GetProductCatalog;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// Product reference data. The catalog read is cached (read-heavy, rarely changes); creating
/// a product invalidates that cache so reads never go stale.
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>Active product catalog (served from cache).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var catalog = await _sender.Send(new GetProductCatalogQuery(), ct);
        return Ok(ApiResponse<IReadOnlyList<ProductDto>>.Ok(catalog));
    }

    /// <summary>Adds a product and invalidates the cached catalog.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = await _sender.Send(
            new CreateProductCommand(request.Sku, request.Name, request.Category, request.UnitPrice), ct);
        return CreatedAtAction(nameof(GetCatalog), ApiResponse<ProductDto>.Ok(product));
    }
}
