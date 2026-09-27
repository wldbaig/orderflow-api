using FluentValidation;

namespace OrderFlow.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    public GetOrdersQueryValidator()
    {
        RuleFor(x => x.Parameters).NotNull().SetValidator(new OrderQueryParametersValidator());
    }
}
