using EmployeeManagement.Api.DTOs.Attendance;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Check in for the day. Employee ID is automatically extracted from JWT token.
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto request)
    {
        var employeeId = User.GetEmployeeId();
        var result = await _attendanceService.CheckInAsync(employeeId, request);
        return CreatedAtAction(nameof(GetAttendance),
            ApiResponse<AttendanceResponseDto>.SuccessResponse(result, "Checked in successfully"));
    }

    /// <summary>
    /// Check out for the day.
    /// </summary>
    [HttpPut("check-out")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutDto request)
    {
        var employeeId = User.GetEmployeeId();
        var result = await _attendanceService.CheckOutAsync(employeeId, request);
        return Ok(ApiResponse<AttendanceResponseDto>.SuccessResponse(result, "Checked out successfully"));
    }

    /// <summary>
    /// Query attendance records with optional filters.
    /// Employees can only view their own records. Admin/Manager can view all.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var currentRole = User.GetRole();
        var currentUserId = User.GetEmployeeId();

        // Employees can only see their own attendance
        if (currentRole == "Employee")
        {
            employeeId = currentUserId;
        }

        var records = await _attendanceService.GetAttendanceAsync(employeeId, departmentId, fromDate, toDate);
        return Ok(ApiResponse<List<AttendanceResponseDto>>.SuccessResponse(records));
    }
}
