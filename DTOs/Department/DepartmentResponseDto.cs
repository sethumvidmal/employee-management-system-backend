namespace EmployeeManagement.Api.DTOs.Department;

public class DepartmentResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
