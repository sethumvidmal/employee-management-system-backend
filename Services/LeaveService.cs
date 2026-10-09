using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Leave;
using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    public async Task<LeaveRequestResponseDto> CreateLeaveRequestAsync(Guid employeeId, CreateLeaveRequestDto request)
    {
        // Validate employee exists and is active
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && e.IsActive);
        if (employee is null)
        {
            throw new KeyNotFoundException($"Employee with ID '{employeeId}' not found or inactive");
        }

        // Parse leave type
        if (!Enum.TryParse<LeaveType>(request.LeaveType, ignoreCase: true, out var leaveType))
        {
            throw new ArgumentException($"Invalid leave type: '{request.LeaveType}'. Must be Annual, Sick, Casual, or Unpaid");
        }

        // Validate date range
        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException("End date must be on or after start date");
        }

        // Check for overlapping leave requests (Pending or Approved)
        var hasOverlap = await _context.LeaveRequests
            .AnyAsync(lr => lr.EmployeeId == employeeId
                && lr.Status != LeaveStatus.Rejected
                && lr.StartDate <= request.EndDate
                && lr.EndDate >= request.StartDate);

        if (hasOverlap)
        {
            throw new InvalidOperationException("You already have a leave request for overlapping dates");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            AppliedOn = DateTimeOffset.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Leave request created by {EmployeeCode}: {LeaveType} from {Start} to {End}",
            employee.EmployeeCode, leaveType, request.StartDate, request.EndDate);

        return new LeaveRequestResponseDto
        {
            Id = leaveRequest.Id,
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            LeaveType = leaveRequest.LeaveType.ToString(),
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status.ToString(),
            ApprovedBy = null,
            AppliedOn = leaveRequest.AppliedOn,
            ReviewedAt = null
        };
    }

    public async Task<LeaveRequestResponseDto> ApproveOrRejectAsync(Guid leaveRequestId, Guid approverId, ApproveRejectLeaveDto request)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

        if (leaveRequest is null)
        {
            throw new KeyNotFoundException($"Leave request with ID '{leaveRequestId}' not found");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Leave request has already been {leaveRequest.Status.ToString().ToLower()}");
        }

        // Cannot approve your own leave request
        if (leaveRequest.EmployeeId == approverId)
        {
            throw new InvalidOperationException("You cannot approve or reject your own leave request");
        }

        leaveRequest.Status = request.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leaveRequest.ApprovedById = approverId;
        leaveRequest.ReviewedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        // Load approver name
        var approver = await _context.Employees.FindAsync(approverId);

        _logger.LogInformation("Leave request {Id} {Status} by {Approver}",
            leaveRequestId, leaveRequest.Status, approver?.Email);

        return new LeaveRequestResponseDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeName = $"{leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName}",
            LeaveType = leaveRequest.LeaveType.ToString(),
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status.ToString(),
            ApprovedBy = approver != null ? $"{approver.FirstName} {approver.LastName}" : null,
            AppliedOn = leaveRequest.AppliedOn,
            ReviewedAt = leaveRequest.ReviewedAt
        };
    }

    public async Task<List<LeaveRequestResponseDto>> GetLeaveRequestsAsync(Guid? employeeId = null, string? status = null)
    {
        var query = _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.ApprovedBy)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(lr => lr.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, ignoreCase: true, out var leaveStatus))
        {
            query = query.Where(lr => lr.Status == leaveStatus);
        }

        return await query
            .OrderByDescending(lr => lr.AppliedOn)
            .Select(lr => new LeaveRequestResponseDto
            {
                Id = lr.Id,
                EmployeeId = lr.EmployeeId,
                EmployeeName = $"{lr.Employee.FirstName} {lr.Employee.LastName}",
                LeaveType = lr.LeaveType.ToString(),
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                Reason = lr.Reason,
                Status = lr.Status.ToString(),
                ApprovedBy = lr.ApprovedBy != null ? $"{lr.ApprovedBy.FirstName} {lr.ApprovedBy.LastName}" : null,
                AppliedOn = lr.AppliedOn,
                ReviewedAt = lr.ReviewedAt
            })
            .ToListAsync();
    }

    public async Task<LeaveRequestResponseDto> GetByIdAsync(Guid id)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.ApprovedBy)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (leaveRequest is null)
        {
            throw new KeyNotFoundException($"Leave request with ID '{id}' not found");
        }

        return new LeaveRequestResponseDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeName = $"{leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName}",
            LeaveType = leaveRequest.LeaveType.ToString(),
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status.ToString(),
            ApprovedBy = leaveRequest.ApprovedBy != null
                ? $"{leaveRequest.ApprovedBy.FirstName} {leaveRequest.ApprovedBy.LastName}"
                : null,
            AppliedOn = leaveRequest.AppliedOn,
            ReviewedAt = leaveRequest.ReviewedAt
        };
    }
}
