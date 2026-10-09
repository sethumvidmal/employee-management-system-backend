namespace EmployeeManagement.Api.Entities;

/// <summary>
/// Stores refresh tokens for JWT token rotation.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    // Foreign Key
    public Guid EmployeeId { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
}
