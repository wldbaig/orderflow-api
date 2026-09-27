namespace OrderFlow.Api.Common;

/// <summary>
/// Consistent envelope for every API response. Success payloads and error payloads share the
/// same shape, so clients always parse the same structure regardless of status code.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };
}

/// <summary>Non-generic error envelope (no data payload).</summary>
public sealed class ApiError
{
    public bool Success => false;
    public string Message { get; init; } = "An error occurred.";
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
