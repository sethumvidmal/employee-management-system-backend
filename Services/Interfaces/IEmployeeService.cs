using EmployeeManagement.Api.DTOs.Employee;

namespace EmployeeManagement.Api.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeResponseDto>> GetAllAsync();
    Task<EmployeeResponseDto> GetByIdAsync(Guid id);
    Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request);
    Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request);
    Task<bool> DeleteAsync(Guid id);
}
