using FluentValidation;

namespace OrderFlow.Application.Orders.Queries.SearchOrdersNl;

public sealed class SearchOrdersNlQueryValidator : AbstractValidator<SearchOrdersNlQuery>
{
    public SearchOrdersNlQueryValidator()
    {
        RuleFor(x => x.Phrase)
            .NotEmpty().WithMessage("A search phrase is required.")
            .MaximumLength(500);
    }
}
