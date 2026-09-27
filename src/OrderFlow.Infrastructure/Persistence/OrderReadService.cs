using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Mapping;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence;

/// <summary>
/// The performance-critical read path. Every query here is <c>AsNoTracking</c>, filters and
/// sorts server-side against indexed columns, projects straight into a flat DTO (so only the
/// needed columns are selected — no full-entity or line materialisation), and pages the
/// results. Sorting is restricted to a whitelist, so no arbitrary expression reaches SQL.
/// </summary>
public sealed class OrderReadService : IOrderReadService
{
    private readonly OrderFlowDbContext _db;

    public OrderReadService(OrderFlowDbContext db) => _db = db;

    public async Task<PagedResult<OrderListItemDto>> ListOrdersAsync(OrderQueryParameters p, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, p);

        var total = await query.LongCountAsync(ct);

        var items = await ApplySort(query, p)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                Status = o.Status,
                Priority = o.Priority,
                TotalAmount = o.TotalAmount,
                TotalUnits = o.TotalUnits,
                LineCount = o.Lines.Count,
                AiAnalysisStatus = o.AiAnalysisStatus,
                AiSummary = o.AiSummary,
                CreatedAtUtc = o.CreatedAtUtc
            })
            .ToListAsync(ct);

        return new PagedResult<OrderListItemDto>(items, p.Page, p.PageSize, total);
    }

    public async Task<OrderDetailDto?> GetOrderDetailAsync(Guid id, CancellationToken ct)
    {
        // Projects the aggregate + lines in a single query; still no change tracking.
        var dto = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.CustomerName, o.CustomerReference, o.SpecialInstructions,
                o.Status, o.Priority, o.TotalAmount, o.TotalUnits, o.CreatedAtUtc, o.UpdatedAtUtc,
                o.AiAnalysisStatus, o.AiSummary, o.AiRiskFlags, o.AiAnalyzedAtUtc,
                Lines = o.Lines.Select(l => new OrderLineDto
                {
                    ProductSku = l.ProductSku,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.Quantity * l.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null) return null;

        return new OrderDetailDto
        {
            Id = dto.Id,
            OrderNumber = dto.OrderNumber,
            CustomerName = dto.CustomerName,
            CustomerReference = dto.CustomerReference,
            SpecialInstructions = dto.SpecialInstructions,
            Status = dto.Status,
            Priority = dto.Priority,
            TotalAmount = dto.TotalAmount,
            TotalUnits = dto.TotalUnits,
            CreatedAtUtc = dto.CreatedAtUtc,
            UpdatedAtUtc = dto.UpdatedAtUtc,
            AiAnalysisStatus = dto.AiAnalysisStatus,
            AiSummary = dto.AiSummary,
            AiRiskFlags = OrderMappings.SplitFlags(dto.AiRiskFlags),
            AiAnalyzedAtUtc = dto.AiAnalyzedAtUtc,
            Lines = dto.Lines
        };
    }

    private static IQueryable<Order> ApplyFilters(IQueryable<Order> query, OrderQueryParameters p)
    {
        if (p.Status.HasValue) query = query.Where(o => o.Status == p.Status.Value);
        if (p.Priority.HasValue) query = query.Where(o => o.Priority == p.Priority.Value);
        if (!string.IsNullOrWhiteSpace(p.CustomerName))
            query = query.Where(o => EF.Functions.Like(o.CustomerName, $"%{p.CustomerName}%"));
        if (p.MinTotalUnits.HasValue) query = query.Where(o => o.TotalUnits >= p.MinTotalUnits.Value);
        if (p.MaxTotalUnits.HasValue) query = query.Where(o => o.TotalUnits <= p.MaxTotalUnits.Value);
        if (p.MinTotalAmount.HasValue) query = query.Where(o => o.TotalAmount >= p.MinTotalAmount.Value);
        if (p.MaxTotalAmount.HasValue) query = query.Where(o => o.TotalAmount <= p.MaxTotalAmount.Value);
        if (p.CreatedAfterUtc.HasValue) query = query.Where(o => o.CreatedAtUtc >= p.CreatedAfterUtc.Value);
        if (p.CreatedBeforeUtc.HasValue) query = query.Where(o => o.CreatedAtUtc <= p.CreatedBeforeUtc.Value);
        if (!string.IsNullOrWhiteSpace(p.RiskFlagContains))
            query = query.Where(o => o.AiRiskFlags != null && EF.Functions.Like(o.AiRiskFlags, $"%{p.RiskFlagContains}%"));
        return query;
    }

    private static IQueryable<Order> ApplySort(IQueryable<Order> query, OrderQueryParameters p)
    {
        // Whitelisted sort key -> a strongly-typed column expression. No string SQL is built.
        Expression<Func<Order, object>> key = p.SortBy switch
        {
            OrderSortField.TotalAmount => o => o.TotalAmount,
            OrderSortField.TotalUnits => o => o.TotalUnits,
            OrderSortField.Priority => o => o.Priority,
            OrderSortField.OrderNumber => o => o.OrderNumber,
            OrderSortField.CustomerName => o => o.CustomerName,
            OrderSortField.Status => o => o.Status,
            _ => o => o.CreatedAtUtc
        };

        // Stable tiebreaker on the unique OrderNumber keeps paging deterministic.
        var ordered = p.SortDescending ? query.OrderByDescending(key) : query.OrderBy(key);
        return ordered.ThenBy(o => o.OrderNumber);
    }
}
