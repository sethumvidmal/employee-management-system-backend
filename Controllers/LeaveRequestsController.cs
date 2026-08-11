using EmployeeManagement.Api.DTOs.Leave;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
// [Authorize]  // TODO: Uncomment after JWT is wired up
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveRequestsController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    /// <summary>
    /// Submit a new leave request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto request)
    {
        // TODO: Extract employeeId from JWT claims
        var employeeId = Guid.Empty; // Placeholder
        var result = await _leaveService.CreateLeaveRequestAsync(employeeId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<LeaveRequestResponseDto>.SuccessResponse(result, "Leave request submitted successfully"));
    }

    /// <summary>
    /// Approve or reject a leave request (Manager/Admin only).
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    // [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveOrReject(Guid id, [FromBody] ApproveRejectLeaveDto request)
    {
        // TODO: Extract approverId from JWT claims
        var approverId = Guid.Empty; // Placeholder
        var result = await _leaveService.ApproveOrRejectAsync(id, approverId, request);
        var message = request.IsApproved ? "Leave request approved" : "Leave request rejected";
        return Ok(ApiResponse<LeaveRequestResponseDto>.SuccessResponse(result, message));
    }

    /// <summary>
    /// Get leave requests with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveRequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, [FromQuery] string? status)
    {
        var requests = await _leaveService.GetLeaveRequestsAsync(employeeId, status);
        return Ok(ApiResponse<List<LeaveRequestResponseDto>>.SuccessResponse(requests));
    }

    /// <summary>
    /// Get a specific leave request by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _leaveService.GetByIdAsync(id);
        return Ok(ApiResponse<LeaveRequestResponseDto>.SuccessResponse(result));
    }
}
