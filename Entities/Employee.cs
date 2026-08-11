using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Entities;

public class Employee
{
    public Guid Id { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Employee;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    // Foreign Keys
    public Guid DepartmentId { get; set; }

    public Guid? ManagerId { get; set; }

    // Navigation Properties
    public Department Department { get; set; } = null!;

    public Employee? Manager { get; set; }

    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; } = new List<LeaveRequest>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
