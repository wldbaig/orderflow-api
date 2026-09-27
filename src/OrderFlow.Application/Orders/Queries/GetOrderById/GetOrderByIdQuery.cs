using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly IOrderReadService _readService;

    public GetOrderByIdQueryHandler(IOrderReadService readService) => _readService = readService;

    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        return await _readService.GetOrderDetailAsync(request.Id, ct)
               ?? throw new NotFoundException(nameof(Domain.Entities.Order), request.Id);
    }
}
