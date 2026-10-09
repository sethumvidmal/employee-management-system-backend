namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Machine-readable error codes returned in <see cref="ApiResponse{T}.ErrorCode"/> for auth failures.
/// Clients should branch on these (e.g. TOKEN_EXPIRED → call /api/auth/refresh,
/// REFRESH_TOKEN_* → send the user back to the login screen).
/// </summary>
public static class AuthErrorCodes
{
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountDeactivated = "ACCOUNT_DEACTIVATED";

    public const string TokenMissing = "TOKEN_MISSING";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string Forbidden = "FORBIDDEN";

    public const string RefreshTokenInvalid = "REFRESH_TOKEN_INVALID";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string RefreshTokenRevoked = "REFRESH_TOKEN_REVOKED";

    public const string ValidationFailed = "VALIDATION_FAILED";
}
