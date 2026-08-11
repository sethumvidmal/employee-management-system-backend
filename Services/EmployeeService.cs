using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Employee;
using EmployeeManagement.Api.Services.Interfaces;

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

    public Task<List<EmployeeResponseDto>> GetAllAsync()
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<EmployeeResponseDto> GetByIdAsync(Guid id)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request)
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
