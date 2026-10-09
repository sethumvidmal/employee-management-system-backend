using EmployeeManagement.Api.DTOs.Leave;

namespace EmployeeManagement.Api.Services.Interfaces;

public interface ILeaveService
{
    Task<LeaveRequestResponseDto> CreateLeaveRequestAsync(Guid employeeId, CreateLeaveRequestDto request);
    Task<LeaveRequestResponseDto> ApproveOrRejectAsync(Guid leaveRequestId, Guid approverId, ApproveRejectLeaveDto request);
    Task<List<LeaveRequestResponseDto>> GetLeaveRequestsAsync(Guid? employeeId = null, string? status = null);
    Task<LeaveRequestResponseDto> GetByIdAsync(Guid id);
}
