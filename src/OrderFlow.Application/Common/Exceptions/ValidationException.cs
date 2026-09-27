using FluentValidation.Results;

namespace OrderFlow.Application.Common.Exceptions;

/// <summary>
/// Aggregates FluentValidation failures thrown by the validation pipeline behavior.
/// Mapped to HTTP 400 with a per-field error dictionary.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
