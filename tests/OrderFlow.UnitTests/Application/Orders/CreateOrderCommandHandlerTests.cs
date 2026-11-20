using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Intelligence;
using OrderFlow.Application.Orders.Commands.CreateOrder;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using Xunit;

namespace OrderFlow.UnitTests.Application.Orders;

public sealed class CreateOrderCommandHandlerTests
{
    private const string OrderNumber = "ORD-2026-000042";

    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IOrderNumberGenerator _numberGenerator = Substitute.For<IOrderNumberGenerator>();
    private readonly IOrderIntelligenceService _intelligence = Substitute.For<IOrderIntelligenceService>();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _numberGenerator.NextAsync(Arg.Any<CancellationToken>()).Returns(OrderNumber);
        _handler = new CreateOrderCommandHandler(_repository, _numberGenerator, _intelligence, NullLogger<CreateOrderCommandHandler>.Instance);
    }

    private static CreateOrderCommand Command() =>
        new CreateOrderCommand("Acme Ltd", "PO-1", "ASAP", new[] { new CreateOrderLineInput("SKU-1", "Widget", 2, 10.50m), new CreateOrderLineInput("SKU-2", "Bolt", 3, 4m) });

    [Fact]
    public async Task Handle_WhenAiSucceeds_AppliesIntelligenceAndPersists()
    {
        _intelligence.AnalyzeOrderAsync(Arg.Any<OrderIntelligenceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new OrderIntelligenceResult(true, "Rush order", OrderPriority.High, new[] { "Tight delivery deadline" }));

        var dto = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(OrderNumber, dto.OrderNumber);
        Assert.Equal(AiAnalysisStatus.Processed, dto.AiAnalysisStatus);
        Assert.Equal(OrderPriority.High, dto.Priority);
        Assert.Equal("Rush order", dto.AiSummary);
        Assert.Equal(new[] { "Tight delivery deadline" }, dto.AiRiskFlags);
        Assert.Equal(33m, dto.TotalAmount);
        Assert.Equal(2, dto.Lines.Count);
        Received.InOrder(() =>
        {
            _repository.AddAsync(Arg.Is<Order>(o => o.OrderNumber == OrderNumber), Arg.Any<CancellationToken>());
            _repository.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenAiFails_MarksIntelligenceFailedAndStillPersists()
    {
        _intelligence.AnalyzeOrderAsync(Arg.Any<OrderIntelligenceRequest>(), Arg.Any<CancellationToken>())
            .Returns(OrderIntelligenceResult.Failed());

        var dto = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(AiAnalysisStatus.Failed, dto.AiAnalysisStatus);
        Assert.Equal(OrderPriority.Medium, dto.Priority);
        Assert.Null(dto.AiSummary);
        Assert.Empty(dto.AiRiskFlags);
        _ = _repository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        _ = _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SendsAggregateTotalsToAi()
    {
        _intelligence.AnalyzeOrderAsync(Arg.Any<OrderIntelligenceRequest>(), Arg.Any<CancellationToken>())
            .Returns(OrderIntelligenceResult.Failed());

        await _handler.Handle(Command(), CancellationToken.None);

        _ = _intelligence.Received(1).AnalyzeOrderAsync(
            Arg.Is<OrderIntelligenceRequest>(r => r.CustomerName == "Acme Ltd" && r.SpecialInstructions == "ASAP" && r.TotalUnits == 5 && r.TotalAmount == 33m && r.LineCount == 2),
            Arg.Any<CancellationToken>());
    }
}
