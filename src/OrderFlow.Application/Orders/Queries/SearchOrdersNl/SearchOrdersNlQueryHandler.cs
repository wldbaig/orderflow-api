using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Queries;

namespace OrderFlow.Application.Orders.Queries.SearchOrdersNl;

/// <summary>
/// Natural-language search. The model NEVER touches the database: it only proposes
/// structured filter parameters, which are then (1) validated with the same validator as
/// the normal endpoint and (2) executed through the same safe read service. If the model
/// is unavailable, we fail loudly (503) rather than run an unbounded query.
/// </summary>
public sealed class SearchOrdersNlQueryHandler : IRequestHandler<SearchOrdersNlQuery, NlSearchResult>
{
    private readonly IOrderIntelligenceService _intelligence;
    private readonly IOrderReadService _readService;
    private readonly ILogger<SearchOrdersNlQueryHandler> _logger;

    public SearchOrdersNlQueryHandler(
        IOrderIntelligenceService intelligence,
        IOrderReadService readService,
        ILogger<SearchOrdersNlQueryHandler> logger)
    {
        _intelligence = intelligence;
        _readService = readService;
        _logger = logger;
    }

    public async Task<NlSearchResult> Handle(SearchOrdersNlQuery request, CancellationToken ct)
    {
        var translation = await _intelligence.TranslateSearchAsync(request.Phrase, ct);
        if (!translation.Succeeded)
            throw new AiUnavailableException("Could not translate the search phrase right now. Please try again or use the structured filters.");

        var parameters = translation.Parameters;

        // Honour client paging over anything the model guessed.
        if (request.Page.HasValue) parameters.Page = request.Page.Value;
        if (request.PageSize.HasValue) parameters.PageSize = request.PageSize.Value;

        // GUARD: hold the model's output to exactly the same rules as normal API input.
        var validation = await new OrderQueryParametersValidator().ValidateAsync(parameters, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Rejected unsafe AI-translated parameters for phrase '{Phrase}': {Errors}",
                request.Phrase, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validation.Errors);
        }

        var results = await _readService.ListOrdersAsync(parameters, ct);
        return new NlSearchResult(request.Phrase, translation.Interpretation, parameters, results);
    }
}
