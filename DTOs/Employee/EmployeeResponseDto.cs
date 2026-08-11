namespace EmployeeManagement.Api.DTOs.Employee;

public class EmployeeResponseDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Department { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Manager { get; set; }
    public Guid? ManagerId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
