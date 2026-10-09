namespace EmployeeManagement.Api.DTOs.Auth;

public class LogoutResponseDto
{
    /// <summary>Number of refresh tokens (sessions) revoked by this call.</summary>
    public int RevokedSessions { get; set; }

    public DateTime LoggedOutAt { get; set; }
}
