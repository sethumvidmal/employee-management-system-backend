namespace EmployeeManagement.Api.DTOs.Employee;

public class CreateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public string Role { get; set; } = "Employee";
}
