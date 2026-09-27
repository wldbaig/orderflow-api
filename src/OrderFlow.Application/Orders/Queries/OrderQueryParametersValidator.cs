using FluentValidation;

namespace OrderFlow.Application.Orders.Queries;

/// <summary>
/// Validates order query parameters. This is the single guard applied to BOTH the normal
/// list endpoint and the AI natural-language search, so untrusted model output is held to
/// exactly the same safety rules as ordinary API input before any query is executed.
/// </summary>
public sealed class OrderQueryParametersValidator : AbstractValidator<OrderQueryParameters>
{
    public OrderQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, OrderQueryParameters.MaxPageSize);

        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority.HasValue);
        RuleFor(x => x.SortBy).IsInEnum();

        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.RiskFlagContains).MaximumLength(200);

        RuleFor(x => x.MinTotalUnits).GreaterThanOrEqualTo(0).When(x => x.MinTotalUnits.HasValue);
        RuleFor(x => x.MaxTotalUnits).GreaterThanOrEqualTo(0).When(x => x.MaxTotalUnits.HasValue);
        RuleFor(x => x.MinTotalAmount).GreaterThanOrEqualTo(0).When(x => x.MinTotalAmount.HasValue);
        RuleFor(x => x.MaxTotalAmount).GreaterThanOrEqualTo(0).When(x => x.MaxTotalAmount.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinTotalUnits.HasValue || !x.MaxTotalUnits.HasValue || x.MinTotalUnits <= x.MaxTotalUnits)
            .WithMessage("MinTotalUnits cannot be greater than MaxTotalUnits.");

        RuleFor(x => x)
            .Must(x => !x.MinTotalAmount.HasValue || !x.MaxTotalAmount.HasValue || x.MinTotalAmount <= x.MaxTotalAmount)
            .WithMessage("MinTotalAmount cannot be greater than MaxTotalAmount.");

        RuleFor(x => x)
            .Must(x => !x.CreatedAfterUtc.HasValue || !x.CreatedBeforeUtc.HasValue || x.CreatedAfterUtc <= x.CreatedBeforeUtc)
            .WithMessage("CreatedAfterUtc cannot be later than CreatedBeforeUtc.");
    }
}
