using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Extension methods for extracting JWT claims from ClaimsPrincipal.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Get the authenticated employee's ID from the "sub" claim.
    /// </summary>
    public static Guid GetEmployeeId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("Invalid or missing employee ID in token");
        }

        return id;
    }

    /// <summary>
    /// Get the authenticated employee's role.
    /// </summary>
    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role)
               ?? throw new UnauthorizedAccessException("Role claim not found in token");
    }

    /// <summary>
    /// Get the authenticated employee's email.
    /// </summary>
    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(JwtRegisteredClaimNames.Email)
               ?? user.FindFirstValue(ClaimTypes.Email)
               ?? throw new UnauthorizedAccessException("Email claim not found in token");
    }
}
