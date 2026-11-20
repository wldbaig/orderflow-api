using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Application.Orders.Queries.SearchOrdersNl;
using OrderFlow.Domain.Enums;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class SearchOrdersNlQueryHandlerTests
{
    private readonly IOrderIntelligenceService _intelligence = Substitute.For<IOrderIntelligenceService>();
    private readonly IOrderReadService _readService = Substitute.For<IOrderReadService>();
    private readonly SearchOrdersNlQueryHandler _handler;

    public SearchOrdersNlQueryHandlerTests()
    {
        _handler = new SearchOrdersNlQueryHandler(_intelligence, _readService, NullLogger<SearchOrdersNlQueryHandler>.Instance);
    }

    private static PagedResult<OrderListItemDto> EmptyPage() =>
        new PagedResult<OrderListItemDto>(Array.Empty<OrderListItemDto>(), 1, 25, 0);

    [Fact]
    public async Task Handle_WhenTranslationFails_ThrowsAiUnavailableAndSkipsQuery()
    {
        _intelligence.TranslateSearchAsync("anything", Arg.Any<CancellationToken>())
            .Returns(new NlFilterTranslation(false, new OrderQueryParameters(), null));

        await Assert.ThrowsAsync<AiUnavailableException>(() => _handler.Handle(new SearchOrdersNlQuery("anything", null, null), CancellationToken.None));

        _ = _readService.DidNotReceive().ListOrdersAsync(Arg.Any<OrderQueryParameters>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTranslated_RunsQueryAndReturnsInterpretation()
    {
        var parameters = new OrderQueryParameters { Priority = OrderPriority.High };
        var page = EmptyPage();
        _intelligence.TranslateSearchAsync("urgent orders", Arg.Any<CancellationToken>())
            .Returns(new NlFilterTranslation(true, parameters, "priority = High"));
        _readService.ListOrdersAsync(parameters, Arg.Any<CancellationToken>()).Returns(page);

        var result = await _handler.Handle(new SearchOrdersNlQuery("urgent orders", null, null), CancellationToken.None);

        Assert.Equal("urgent orders", result.Phrase);
        Assert.Equal("priority = High", result.Interpretation);
        Assert.Same(parameters, result.AppliedParameters);
        Assert.Same(page, result.Results);
    }

    [Fact]
    public async Task Handle_WithClientPaging_OverridesTranslatedPaging()
    {
        var parameters = new OrderQueryParameters { Page = 1, PageSize = 25 };
        _intelligence.TranslateSearchAsync("orders", Arg.Any<CancellationToken>())
            .Returns(new NlFilterTranslation(true, parameters, null));
        _readService.ListOrdersAsync(Arg.Any<OrderQueryParameters>(), Arg.Any<CancellationToken>()).Returns(EmptyPage());

        await _handler.Handle(new SearchOrdersNlQuery("orders", 3, 10), CancellationToken.None);

        _ = _readService.Received(1).ListOrdersAsync(Arg.Is<OrderQueryParameters>(p => p.Page == 3 && p.PageSize == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAiReturnsInconsistentFilters_RejectsWithValidationException()
    {
        var parameters = new OrderQueryParameters { MinTotalUnits = 10, MaxTotalUnits = 5 };
        _intelligence.TranslateSearchAsync("weird", Arg.Any<CancellationToken>())
            .Returns(new NlFilterTranslation(true, parameters, null));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(new SearchOrdersNlQuery("weird", null, null), CancellationToken.None));

        Assert.NotEmpty(exception.Errors);
        _ = _readService.DidNotReceive().ListOrdersAsync(Arg.Any<OrderQueryParameters>(), Arg.Any<CancellationToken>());
    }
}
