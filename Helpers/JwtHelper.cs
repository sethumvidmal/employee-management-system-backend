using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Generates and validates JWT tokens.
/// </summary>
public class JwtHelper
{
    private readonly JwtSettings _settings;

    public JwtHelper(JwtSettings settings)
    {
        _settings = settings;
    }

    public int ExpirationInSeconds => _settings.ExpirationInMinutes * 60;

    /// <summary>
    /// Generate a signed JWT for the given employee.
    /// Claims: sub (employee ID), email, role, full name.
    /// Returns the token together with its exact expiry so responses match the "exp" claim.
    /// </summary>
    public (string Token, DateTime ExpiresAt) GenerateToken(Employee employee)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, employee.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, employee.Role.ToString()),
            new("fullName", $"{employee.FirstName} {employee.LastName}")
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
