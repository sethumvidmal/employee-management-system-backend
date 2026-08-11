namespace EmployeeManagement.Api.DTOs.Leave;

public class CreateLeaveRequestDto
{
    public string LeaveType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
}
