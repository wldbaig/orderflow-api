namespace OrderFlow.Application.Common.Exceptions;

/// <summary>Raised when a requested entity does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
    }
}
