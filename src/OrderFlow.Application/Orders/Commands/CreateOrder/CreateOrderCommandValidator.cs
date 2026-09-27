using FluentValidation;

namespace OrderFlow.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200);

        RuleFor(x => x.CustomerReference)
            .MaximumLength(100);

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(4000);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("An order must have at least one line.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductSku).NotEmpty().MaximumLength(50);
            line.RuleFor(l => l.ProductName).MaximumLength(200);
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
        });
    }
}
