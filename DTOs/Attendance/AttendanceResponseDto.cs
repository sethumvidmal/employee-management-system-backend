namespace EmployeeManagement.Api.DTOs.Attendance;

public class AttendanceResponseDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTimeOffset CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public DateOnly WorkDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
}
