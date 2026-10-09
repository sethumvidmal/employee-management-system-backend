using EmployeeManagement.Api.DTOs.Attendance;

namespace EmployeeManagement.Api.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceResponseDto> CheckInAsync(Guid employeeId, CheckInDto request);
    Task<AttendanceResponseDto> CheckOutAsync(Guid employeeId, CheckOutDto request);
    Task<List<AttendanceResponseDto>> GetAttendanceAsync(
        Guid? employeeId = null,
        Guid? departmentId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);
}
