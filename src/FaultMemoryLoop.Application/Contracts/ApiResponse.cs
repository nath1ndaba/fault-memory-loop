namespace FaultMemoryLoop.Application.Contracts;

/// <summary>
/// Consistent response envelope for every endpoint, so any client — including
/// a future frontend — can rely on the same success/error/metadata shape
/// regardless of which endpoint it's calling.
/// </summary>
public record ApiResponse<T>(bool Success, T? Data, string? Error, DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Ok(T data) =>
        new(true, data, null, DateTimeOffset.UtcNow);

    public static ApiResponse<T> Fail(string error) =>
        new(false, default, error, DateTimeOffset.UtcNow);
}
