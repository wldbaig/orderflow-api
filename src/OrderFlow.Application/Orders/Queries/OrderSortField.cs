namespace OrderFlow.Application.Orders.Queries;

/// <summary>
/// Whitelisted sort columns. Sorting is restricted to this enum so that neither an API
/// caller nor the AI natural-language translator can inject an arbitrary sort expression.
/// </summary>
public enum OrderSortField
{
    CreatedAtUtc = 0,
    TotalAmount = 1,
    TotalUnits = 2,
    Priority = 3,
    OrderNumber = 4,
    CustomerName = 5,
    Status = 6
}
