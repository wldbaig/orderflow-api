namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Produces unique, human-friendly order numbers (e.g. <c>ORD-2026-000123</c>).</summary>
public interface IOrderNumberGenerator
{
    Task<string> NextAsync(CancellationToken ct);
}
