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

        var token = _jwtHelper.GenerateToken(employee);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return new LoginResponseDto
        {
            Token = token,
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

        var token = _jwtHelper.GenerateToken(employee);

        _logger.LogInformation("New employee registered: {EmployeeCode} - {Email} as {Role}",
            employee.EmployeeCode, employee.Email, employee.Role);

        return new LoginResponseDto
        {
            Token = token,
            Email = employee.Email,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Role = employee.Role.ToString(),
            ExpiresAt = _jwtHelper.GetExpiration()
        };
    }
}
