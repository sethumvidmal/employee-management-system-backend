namespace EmployeeManagement.Api.DTOs.Department;

public class UpdateDepartmentDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
