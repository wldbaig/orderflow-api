namespace OrderFlow.Domain.Common;

/// <summary>
/// Thrown when a domain invariant or business rule is violated.
/// The API layer translates this into an HTTP 409/422 response, keeping
/// business-rule enforcement inside the domain rather than in controllers.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
