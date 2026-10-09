namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Authentication / authorization failure carrying an HTTP status and a machine-readable error code.
/// Translated into an <see cref="ApiResponse{T}"/> by the global exception middleware.
/// </summary>
public class AuthException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AuthException(string message, string errorCode, int statusCode = StatusCodes.Status401Unauthorized)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
