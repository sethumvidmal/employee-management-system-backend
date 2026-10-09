using System.Security.Cryptography;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Auth;
using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthService> _logger;

    private const int RefreshTokenExpiryDays = 7;

    // Verified against when the email is unknown so both failure paths take the same time
    // (prevents discovering valid emails by measuring response time).
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    public AuthService(AppDbContext context, JwtHelper jwtHelper, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == email);

        if (employee is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash);
            _logger.LogWarning("Login failed: no account found for {Email}", email);
            throw new AuthException("Invalid email or password", AuthErrorCodes.InvalidCredentials);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
        {
            _logger.LogWarning("Login failed: incorrect password for {Email}", email);
            throw new AuthException("Invalid email or password", AuthErrorCodes.InvalidCredentials);
        }

        // Only revealed after the correct password was supplied, so it does not leak account existence.
        if (!employee.IsActive)
        {
            _logger.LogWarning("Login blocked: account {Email} is deactivated", email);
            throw new AuthException(
                "Your account has been deactivated. Please contact an administrator.",
                AuthErrorCodes.AccountDeactivated,
                StatusCodes.Status403Forbidden);
        }

        var response = await IssueTokensAsync(employee);

        _logger.LogInformation("User {Email} logged in successfully", email);

        return response;
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // Check for duplicate email
        var existingEmployee = await _context.Employees
            .AnyAsync(e => e.Email == request.Email);

        if (existingEmployee)
        {
            throw new InvalidOperationException($"An employee with email '{request.Email}' already exists");
        }

        // Validate department exists
        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
        if (!departmentExists)
        {
            throw new KeyNotFoundException($"Department with ID '{request.DepartmentId}' not found");
        }

        // Validate manager exists (if provided)
        if (request.ManagerId.HasValue)
        {
            var managerExists = await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Manager with ID '{request.ManagerId}' not found");
            }
        }

        // Parse role
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            throw new ArgumentException($"Invalid role: '{request.Role}'. Must be Admin, Manager, or Employee");
        }

        // Generate employee code
        var employeeCount = await _context.Employees.CountAsync();
        var employeeCode = $"EMP{(employeeCount + 1):D3}";

        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            Role = role,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var response = await IssueTokensAsync(employee);

        _logger.LogInformation("New employee registered: {EmployeeCode} - {Email} as {Role}",
            employee.EmployeeCode, employee.Email, employee.Role);

        return response;
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.Employee)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken is null)
        {
            throw new AuthException("Invalid refresh token. Please login again.", AuthErrorCodes.RefreshTokenInvalid);
        }

        if (storedToken.IsRevoked)
        {
            throw new AuthException("Refresh token has been revoked. Please login again.", AuthErrorCodes.RefreshTokenRevoked);
        }

        if (storedToken.IsExpired)
        {
            throw new AuthException("Refresh token has expired. Please login again.", AuthErrorCodes.RefreshTokenExpired);
        }

        if (!storedToken.Employee.IsActive)
        {
            storedToken.RevokedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            throw new AuthException(
                "Your account has been deactivated. Please contact an administrator.",
                AuthErrorCodes.AccountDeactivated,
                StatusCodes.Status403Forbidden);
        }

        // Revoke the old refresh token (token rotation) — saved together with the new one
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var response = await IssueTokensAsync(storedToken.Employee);

        _logger.LogInformation("Token refreshed for {Email}", storedToken.Employee.Email);

        return response;
    }

    public async Task<LogoutResponseDto> LogoutAsync(LogoutRequestDto request)
    {
        var now = DateTimeOffset.UtcNow;

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        // Logout is idempotent: an unknown or already-revoked token still results in a logged-out client.
        if (storedToken is null || storedToken.IsRevoked)
        {
            return new LogoutResponseDto { RevokedSessions = 0, LoggedOutAt = now.UtcDateTime };
        }

        storedToken.RevokedAt = now;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} logged out", storedToken.EmployeeId);

        return new LogoutResponseDto { RevokedSessions = 1, LoggedOutAt = now.UtcDateTime };
    }

    public async Task<LogoutResponseDto> LogoutAllAsync(Guid employeeId)
    {
        var now = DateTimeOffset.UtcNow;

        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.EmployeeId == employeeId && rt.RevokedAt == null && rt.ExpiresAt > now)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} logged out of all sessions ({Count} revoked)",
            employeeId, activeTokens.Count);

        return new LogoutResponseDto { RevokedSessions = activeTokens.Count, LoggedOutAt = now.UtcDateTime };
    }

    /// <summary>
    /// Create an access token + persisted refresh token and build the auth response.
    /// </summary>
    private async Task<LoginResponseDto> IssueTokensAsync(Employee employee)
    {
        var (accessToken, accessTokenExpiresAt) = _jwtHelper.GenerateToken(employee);
        var refreshToken = await CreateRefreshTokenAsync(employee.Id);

        return new LoginResponseDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            Email = employee.Email,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Role = employee.Role.ToString(),
            TokenType = "Bearer",
            AccessToken = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            ExpiresIn = _jwtHelper.ExpirationInSeconds,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt.UtcDateTime
        };
    }

    /// <summary>
    /// Generate a cryptographically secure refresh token and store it in the database.
    /// </summary>
    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid employeeId)
    {
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedAt = DateTimeOffset.UtcNow,
            EmployeeId = employeeId
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }
}
