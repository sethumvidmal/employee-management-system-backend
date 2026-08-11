using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Employee;
using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(AppDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EmployeeResponseDto>> GetAllAsync()
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Select(e => new EmployeeResponseDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                Department = e.Department.Name,
                DepartmentId = e.DepartmentId,
                Role = e.Role.ToString(),
                Manager = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
                ManagerId = e.ManagerId,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<EmployeeResponseDto> GetByIdAsync(Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            throw new KeyNotFoundException($"Employee with ID '{id}' not found");
        }

        return MapToDto(employee);
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request)
    {
        // Check for duplicate email
        var emailExists = await _context.Employees.AnyAsync(e => e.Email == request.Email);
        if (emailExists)
        {
            throw new InvalidOperationException($"An employee with email '{request.Email}' already exists");
        }

        // Validate department exists
        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId && d.IsActive);
        if (!departmentExists)
        {
            throw new KeyNotFoundException($"Department with ID '{request.DepartmentId}' not found or inactive");
        }

        // Validate manager exists (if provided)
        if (request.ManagerId.HasValue)
        {
            var managerExists = await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value && e.IsActive);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Manager with ID '{request.ManagerId}' not found or inactive");
            }
        }

        // Parse role
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            throw new ArgumentException($"Invalid role: '{request.Role}'. Must be Admin, Manager, or Employee");
        }

        // Auto-generate employee code
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

        // Reload with navigation properties
        await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        if (employee.ManagerId.HasValue)
            await _context.Entry(employee).Reference(e => e.Manager).LoadAsync();

        _logger.LogInformation("Employee created: {EmployeeCode} - {Email}", employee.EmployeeCode, employee.Email);

        return MapToDto(employee);
    }

    public async Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            throw new KeyNotFoundException($"Employee with ID '{id}' not found");
        }

        // Update email (check uniqueness)
        if (!string.IsNullOrEmpty(request.Email) && request.Email != employee.Email)
        {
            var emailExists = await _context.Employees.AnyAsync(e => e.Email == request.Email && e.Id != id);
            if (emailExists)
            {
                throw new InvalidOperationException($"An employee with email '{request.Email}' already exists");
            }
            employee.Email = request.Email;
        }

        // Update department (check existence)
        if (request.DepartmentId.HasValue && request.DepartmentId.Value != employee.DepartmentId)
        {
            var deptExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value && d.IsActive);
            if (!deptExists)
            {
                throw new KeyNotFoundException($"Department with ID '{request.DepartmentId}' not found or inactive");
            }
            employee.DepartmentId = request.DepartmentId.Value;
        }

        // Update manager (check existence)
        if (request.ManagerId.HasValue)
        {
            if (request.ManagerId.Value == id)
            {
                throw new InvalidOperationException("An employee cannot be their own manager");
            }
            var managerExists = await _context.Employees.AnyAsync(e => e.Id == request.ManagerId.Value && e.IsActive);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Manager with ID '{request.ManagerId}' not found or inactive");
            }
            employee.ManagerId = request.ManagerId.Value;
        }

        // Update role
        if (!string.IsNullOrEmpty(request.Role))
        {
            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            {
                throw new ArgumentException($"Invalid role: '{request.Role}'. Must be Admin, Manager, or Employee");
            }
            employee.Role = role;
        }

        if (!string.IsNullOrEmpty(request.FirstName))
            employee.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            employee.LastName = request.LastName;

        if (request.PhoneNumber is not null)
            employee.PhoneNumber = request.PhoneNumber;

        if (request.IsActive.HasValue)
            employee.IsActive = request.IsActive.Value;

        employee.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        // Reload navigation properties after potential FK changes
        await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        if (employee.ManagerId.HasValue)
            await _context.Entry(employee).Reference(e => e.Manager).LoadAsync();

        _logger.LogInformation("Employee updated: {EmployeeCode} ({Id})", employee.EmployeeCode, employee.Id);

        return MapToDto(employee);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee is null)
        {
            throw new KeyNotFoundException($"Employee with ID '{id}' not found");
        }

        // Soft delete
        employee.IsActive = false;
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee soft-deleted: {EmployeeCode} ({Id})", employee.EmployeeCode, employee.Id);

        return true;
    }

    private static EmployeeResponseDto MapToDto(Employee employee)
    {
        return new EmployeeResponseDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Department = employee.Department?.Name ?? string.Empty,
            DepartmentId = employee.DepartmentId,
            Role = employee.Role.ToString(),
            Manager = employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
            ManagerId = employee.ManagerId,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt
        };
    }
}
