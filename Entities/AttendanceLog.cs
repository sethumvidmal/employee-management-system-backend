using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Entities;

public class AttendanceLog
{
    public Guid Id { get; set; }

    public DateTimeOffset CheckInTime { get; set; }

    public DateTimeOffset? CheckOutTime { get; set; }

    public DateOnly WorkDate { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public DeviceType DeviceType { get; set; } = DeviceType.Web;

    // Foreign Key
    public Guid EmployeeId { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
}
