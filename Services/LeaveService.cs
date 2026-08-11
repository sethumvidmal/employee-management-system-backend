using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Leave;
using EmployeeManagement.Api.Services.Interfaces;

namespace EmployeeManagement.Api.Services;

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(AppDbContext context, ILogger<LeaveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<LeaveRequestResponseDto> CreateLeaveRequestAsync(Guid employeeId, CreateLeaveRequestDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<LeaveRequestResponseDto> ApproveOrRejectAsync(Guid leaveRequestId, Guid approverId, ApproveRejectLeaveDto request)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<List<LeaveRequestResponseDto>> GetLeaveRequestsAsync(Guid? employeeId = null, string? status = null)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public Task<LeaveRequestResponseDto> GetByIdAsync(Guid id)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
}
