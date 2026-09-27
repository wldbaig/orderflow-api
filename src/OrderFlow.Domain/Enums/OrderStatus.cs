namespace OrderFlow.Domain.Enums;

/// <summary>Lifecycle state of an order. Transitions are enforced by the <c>Order</c> aggregate.</summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Completed = 3,
    Cancelled = 4
}
