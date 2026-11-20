using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OrderFlow.Api.Common;
using OrderFlow.Api.Contracts;
using OrderFlow.Api.Controllers;
using OrderFlow.Application.Reference.Commands.CreateProduct;
using OrderFlow.Application.Reference.Dtos;
using OrderFlow.Application.Reference.Queries.GetProductCatalog;
using Xunit;

namespace OrderFlow.UnitTests.Api;

public sealed class ProductsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_sender);
    }

    [Fact]
    public async Task GetCatalog_ReturnsCatalogInEnvelope()
    {
        IReadOnlyList<ProductDto> catalog = new[] { new ProductDto { Sku = "SKU-1" } };
        _sender.Send(Arg.Any<GetProductCatalogQuery>(), Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await _controller.GetCatalog(CancellationToken.None);

        var payload = Assert.IsType<ApiResponse<IReadOnlyList<ProductDto>>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Same(catalog, payload.Data);
    }

    [Fact]
    public async Task Create_MapsRequestAndReturns201()
    {
        var dto = new ProductDto { Sku = "SKU-1" };
        _sender.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.Create(new CreateProductRequest("SKU-1", "Widget", "Hardware", 2m), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(dto, Assert.IsType<ApiResponse<ProductDto>>(created.Value).Data);
        _ = _sender.Received(1).Send(Arg.Is<CreateProductCommand>(c => c.Sku == "SKU-1" && c.Name == "Widget" && c.Category == "Hardware" && c.UnitPrice == 2m), Arg.Any<CancellationToken>());
    }
}
