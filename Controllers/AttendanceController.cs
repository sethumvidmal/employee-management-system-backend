using EmployeeManagement.Api.DTOs.Attendance;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]  // TODO: Uncomment after JWT is wired up
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Check in for the day. Employee ID derived from JWT claims.
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto request)
    {
        // TODO: Extract employeeId from JWT claims
        var employeeId = Guid.Empty; // Placeholder
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
        // TODO: Extract employeeId from JWT claims
        var employeeId = Guid.Empty; // Placeholder
        var result = await _attendanceService.CheckOutAsync(employeeId, request);
        return Ok(ApiResponse<AttendanceResponseDto>.SuccessResponse(result, "Checked out successfully"));
    }

    /// <summary>
    /// Query attendance records with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var records = await _attendanceService.GetAttendanceAsync(employeeId, departmentId, fromDate, toDate);
        return Ok(ApiResponse<List<AttendanceResponseDto>>.SuccessResponse(records));
    }
}
