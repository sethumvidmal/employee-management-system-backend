using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Attendance;
using EmployeeManagement.Api.Services.Interfaces;

namespace EmployeeManagement.Api.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(AppDbContext context, ILogger<AttendanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<AttendanceResponseDto> CheckInAsync(Guid employeeId, CheckInDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<AttendanceResponseDto> CheckOutAsync(Guid employeeId, CheckOutDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<List<AttendanceResponseDto>> GetAttendanceAsync(
        Guid? employeeId = null,
        Guid? departmentId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
}
