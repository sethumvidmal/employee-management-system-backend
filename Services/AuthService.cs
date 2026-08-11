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

    public AuthService(AppDbContext context, JwtHelper jwtHelper, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == request.Email && e.IsActive);

        if (employee is null)
        {
            _logger.LogWarning("Login failed: no active account found for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
        {
            _logger.LogWarning("Login failed: incorrect password for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var accessToken = _jwtHelper.GenerateToken(employee);
        var refreshToken = await CreateRefreshTokenAsync(employee.Id);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            Email = employee.Email,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Role = employee.Role.ToString(),
            ExpiresAt = _jwtHelper.GetExpiration()
        };
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

        var accessToken = _jwtHelper.GenerateToken(employee);
        var refreshToken = await CreateRefreshTokenAsync(employee.Id);

        _logger.LogInformation("New employee registered: {EmployeeCode} - {Email} as {Role}",
            employee.EmployeeCode, employee.Email, employee.Role);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            Email = employee.Email,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Role = employee.Role.ToString(),
            ExpiresAt = _jwtHelper.GetExpiration()
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.Employee)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (storedToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Refresh token has expired. Please login again");
        }

        if (storedToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked");
        }

        if (!storedToken.Employee.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated");
        }

        // Revoke the old refresh token (token rotation)
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        // Issue new tokens
        var accessToken = _jwtHelper.GenerateToken(storedToken.Employee);
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.EmployeeId);

        _logger.LogInformation("Token refreshed for {Email}", storedToken.Employee.Email);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            Email = storedToken.Employee.Email,
            FullName = $"{storedToken.Employee.FirstName} {storedToken.Employee.LastName}",
            Role = storedToken.Employee.Role.ToString(),
            ExpiresAt = _jwtHelper.GetExpiration()
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
