using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Department;
using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(AppDbContext context, ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        return await _context.Departments
            .Select(d => new DepartmentResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                EmployeeCount = d.Employees.Count(e => e.IsActive),
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
        {
            throw new KeyNotFoundException($"Department with ID '{id}' not found");
        }

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            EmployeeCount = department.Employees.Count(e => e.IsActive),
            CreatedAt = department.CreatedAt
        };
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto request)
    {
        // Check for duplicate name
        var exists = await _context.Departments.AnyAsync(d => d.Name == request.Name);
        if (exists)
        {
            throw new InvalidOperationException($"A department with name '{request.Name}' already exists");
        }

        var department = new Department
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department created: {Name} ({Id})", department.Name, department.Id);

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            EmployeeCount = 0,
            CreatedAt = department.CreatedAt
        };
    }

    public async Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto request)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department is null)
        {
            throw new KeyNotFoundException($"Department with ID '{id}' not found");
        }

        // Check for duplicate name if name is being changed
        if (!string.IsNullOrEmpty(request.Name) && request.Name != department.Name)
        {
            var nameExists = await _context.Departments.AnyAsync(d => d.Name == request.Name && d.Id != id);
            if (nameExists)
            {
                throw new InvalidOperationException($"A department with name '{request.Name}' already exists");
            }
            department.Name = request.Name;
        }

        if (request.Description is not null)
            department.Description = request.Description;

        if (request.IsActive.HasValue)
            department.IsActive = request.IsActive.Value;

        department.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Department updated: {Name} ({Id})", department.Name, department.Id);

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            EmployeeCount = department.Employees.Count(e => e.IsActive),
            CreatedAt = department.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var department = await _context.Departments.FindAsync(id);

        if (department is null)
        {
            throw new KeyNotFoundException($"Department with ID '{id}' not found");
        }

        // Soft delete
        department.IsActive = false;
        department.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department soft-deleted: {Name} ({Id})", department.Name, department.Id);

        return true;
    }
}
