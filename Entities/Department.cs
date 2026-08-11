using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Entities;

public class Department
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
