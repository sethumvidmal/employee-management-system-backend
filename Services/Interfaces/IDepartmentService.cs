using EmployeeManagement.Api.DTOs.Department;

namespace EmployeeManagement.Api.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentResponseDto>> GetAllAsync();
    Task<DepartmentResponseDto> GetByIdAsync(Guid id);
    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto request);
    Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto request);
    Task<bool> DeleteAsync(Guid id);
}
