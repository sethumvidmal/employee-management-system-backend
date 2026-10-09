namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Standardized API response wrapper for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code (e.g. TOKEN_EXPIRED) so clients can react without parsing messages.
    /// Only set on failures.
    /// </summary>
    public string? ErrorCode { get; set; }

    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResponse(T data, string message = "Request completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors ?? new List<string>()
        };
    }
}
