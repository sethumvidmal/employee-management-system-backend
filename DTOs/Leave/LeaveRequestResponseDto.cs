namespace EmployeeManagement.Api.DTOs.Leave;

public class LeaveRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset AppliedOn { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
