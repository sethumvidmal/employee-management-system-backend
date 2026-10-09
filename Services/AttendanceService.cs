using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Attendance;
using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly WorkClock _clock;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(AppDbContext context, WorkClock clock, ILogger<AttendanceService> logger)
    {
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AttendanceResponseDto> CheckInAsync(Guid employeeId, CheckInDto request)
    {
        // Validate employee exists and is active
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && e.IsActive);
        if (employee is null)
        {
            throw new KeyNotFoundException($"Employee with ID '{employeeId}' not found or inactive");
        }

        // Prevent duplicate check-in for the same day.
        // "Today" is resolved in the organisation's local timezone, not UTC — an
        // early-morning check-in must not be attributed to the previous work date.
        var checkInTime = _clock.UtcNow;
        var today = _clock.ToWorkDate(checkInTime);
        var alreadyCheckedIn = await _context.AttendanceLogs
            .AnyAsync(a => a.EmployeeId == employeeId && a.WorkDate == today);

        if (alreadyCheckedIn)
        {
            throw new InvalidOperationException("You have already checked in today");
        }

        // Parse device type
        if (!Enum.TryParse<DeviceType>(request.DeviceType, ignoreCase: true, out var deviceType))
        {
            throw new ArgumentException($"Invalid device type: '{request.DeviceType}'. Must be Web or Mobile");
        }

        var attendanceLog = new AttendanceLog
        {
            EmployeeId = employeeId,
            CheckInTime = checkInTime,
            WorkDate = today,
            Status = AttendanceStatus.Present,
            DeviceType = deviceType
        };

        _context.AttendanceLogs.Add(attendanceLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeCode} checked in at {Time}",
            employee.EmployeeCode, attendanceLog.CheckInTime);

        return new AttendanceResponseDto
        {
            Id = attendanceLog.Id,
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            CheckInTime = attendanceLog.CheckInTime,
            CheckOutTime = null,
            WorkDate = attendanceLog.WorkDate,
            Status = attendanceLog.Status.ToString(),
            DeviceType = attendanceLog.DeviceType.ToString()
        };
    }

    public async Task<AttendanceResponseDto> CheckOutAsync(Guid employeeId, CheckOutDto request)
    {
        // Find the open attendance log (checked in but not yet checked out)
        var attendanceLog = await _context.AttendanceLogs
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceLogId && a.EmployeeId == employeeId);

        if (attendanceLog is null)
        {
            throw new KeyNotFoundException("Attendance log not found or does not belong to you");
        }

        if (attendanceLog.CheckOutTime.HasValue)
        {
            throw new InvalidOperationException("You have already checked out for this record");
        }

        attendanceLog.CheckOutTime = _clock.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeCode} checked out at {Time}",
            attendanceLog.Employee.EmployeeCode, attendanceLog.CheckOutTime);

        return new AttendanceResponseDto
        {
            Id = attendanceLog.Id,
            EmployeeId = attendanceLog.EmployeeId,
            EmployeeName = $"{attendanceLog.Employee.FirstName} {attendanceLog.Employee.LastName}",
            CheckInTime = attendanceLog.CheckInTime,
            CheckOutTime = attendanceLog.CheckOutTime,
            WorkDate = attendanceLog.WorkDate,
            Status = attendanceLog.Status.ToString(),
            DeviceType = attendanceLog.DeviceType.ToString()
        };
    }

    public async Task<List<AttendanceResponseDto>> GetAttendanceAsync(
        Guid? employeeId = null,
        Guid? departmentId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        var query = _context.AttendanceLogs
            .Include(a => a.Employee)
            .AsQueryable();

        // Apply filters dynamically
        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.WorkDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.WorkDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.WorkDate)
            .ThenByDescending(a => a.CheckInTime)
            .Select(a => new AttendanceResponseDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                WorkDate = a.WorkDate,
                Status = a.Status.ToString(),
                DeviceType = a.DeviceType.ToString()
            })
            .ToListAsync();
    }
}
