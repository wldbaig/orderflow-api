using MediatR;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;

namespace OrderFlow.Application.Orders.Queries.SearchOrdersNl;

public sealed record SearchOrdersNlQuery(string Phrase, int? Page, int? PageSize)
    : IRequest<NlSearchResult>;

/// <summary>
/// The NL search response. Echoes back the model's interpretation and the exact structured
/// parameters that were validated and run, so the translation is transparent and auditable.
/// </summary>
public sealed record NlSearchResult(
    string Phrase,
    string? Interpretation,
    OrderQueryParameters AppliedParameters,
    PagedResult<OrderListItemDto> Results);
