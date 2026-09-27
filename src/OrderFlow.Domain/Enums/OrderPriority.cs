namespace OrderFlow.Domain.Enums;

/// <summary>Business priority assigned to an order (by the AI intake step, or defaulted).</summary>
public enum OrderPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
