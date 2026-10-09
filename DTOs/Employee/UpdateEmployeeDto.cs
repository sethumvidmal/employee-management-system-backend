namespace EmployeeManagement.Api.DTOs.Employee;

public class UpdateEmployeeDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
