using EmployeeManagement.Api.DTOs.Department;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")]  // TODO: Uncomment after JWT is wired up
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    /// <summary>
    /// Get all departments.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _departmentService.GetAllAsync();
        return Ok(ApiResponse<List<DepartmentResponseDto>>.SuccessResponse(departments));
    }

    /// <summary>
    /// Get department by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var department = await _departmentService.GetByIdAsync(id);
        return Ok(ApiResponse<DepartmentResponseDto>.SuccessResponse(department));
    }

    /// <summary>
    /// Create a new department.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto request)
    {
        var department = await _departmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = department.Id },
            ApiResponse<DepartmentResponseDto>.SuccessResponse(department, "Department created successfully"));
    }

    /// <summary>
    /// Update an existing department.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto request)
    {
        var department = await _departmentService.UpdateAsync(id, request);
        return Ok(ApiResponse<DepartmentResponseDto>.SuccessResponse(department, "Department updated successfully"));
    }

    /// <summary>
    /// Delete a department.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _departmentService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Department deleted successfully"));
    }
}
