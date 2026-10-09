using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Entities;

public class LeaveRequest
{
    public Guid Id { get; set; }

    public LeaveType LeaveType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Reason { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public DateTimeOffset AppliedOn { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }

    // Foreign Keys
    public Guid EmployeeId { get; set; }

    public Guid? ApprovedById { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;

    public Employee? ApprovedBy { get; set; }
}
