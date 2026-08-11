using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Department;
using EmployeeManagement.Api.Services.Interfaces;

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

    public Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
}
