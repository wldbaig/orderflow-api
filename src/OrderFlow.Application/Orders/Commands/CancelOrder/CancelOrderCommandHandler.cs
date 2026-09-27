using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Mapping;

namespace OrderFlow.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDetailDto>
{
    private readonly IOrderRepository _repository;

    public CancelOrderCommandHandler(IOrderRepository repository) => _repository = repository;

    public async Task<OrderDetailDto> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, ct)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Order), request.OrderId);

        // Business rule enforced by the aggregate; throws DomainException if not allowed.
        order.Cancel();

        await _repository.SaveChangesAsync(ct);
        return order.ToDetailDto();
    }
}
