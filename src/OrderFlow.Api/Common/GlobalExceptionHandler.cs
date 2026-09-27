using Microsoft.AspNetCore.Diagnostics;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Domain.Common;
using ValidationException = OrderFlow.Application.Common.Exceptions.ValidationException;

namespace OrderFlow.Api.Common;

/// <summary>
/// Single place that turns exceptions into HTTP responses. Handlers and controllers just
/// throw meaningful exceptions; this maps each to the right status code and the standard
/// <see cref="ApiError"/> envelope. Uses the .NET 8 <see cref="IExceptionHandler"/> pipeline.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, message, errors) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogInformation("Request rejected ({Status}): {Message}", status, message);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ApiError { Message = message, Errors = errors }, ct);
        return true;
    }

    private static (int Status, string Message, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception ex) => ex switch
    {
        ValidationException v => (StatusCodes.Status400BadRequest, v.Message, v.Errors),
        NotFoundException n => (StatusCodes.Status404NotFound, n.Message, null),
        DomainException d => (StatusCodes.Status409Conflict, d.Message, null),
        AiUnavailableException a => (StatusCodes.Status503ServiceUnavailable, a.Message, null),
        OperationCanceledException => (499 /* client closed request */, "The request was cancelled.", null),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
    };
}
