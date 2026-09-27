namespace OrderFlow.Application.Common.Exceptions;

/// <summary>
/// Raised when an AI feature that a request strictly depends on (e.g. natural-language
/// search translation) cannot be completed. Mapped to HTTP 503. Note this is NOT used by
/// the order-create flow, where AI failure is tolerated and the order is still saved.
/// </summary>
public sealed class AiUnavailableException : Exception
{
    public AiUnavailableException(string message) : base(message)
    {
    }
}
