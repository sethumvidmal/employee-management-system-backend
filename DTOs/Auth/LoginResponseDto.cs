namespace EmployeeManagement.Api.DTOs.Auth;

public class LoginResponseDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Access token expiry (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Access token lifetime in seconds.</summary>
    public int ExpiresIn { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Refresh token expiry (UTC). After this the user must log in again.</summary>
    public DateTime RefreshTokenExpiresAt { get; set; }
}
