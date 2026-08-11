using EmployeeManagement.Api.DTOs.Employee;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]  // TODO: Uncomment after JWT is wired up
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Get all employees (Admin, Manager).
    /// </summary>
    [HttpGet]
    // [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(ApiResponse<List<EmployeeResponseDto>>.SuccessResponse(employees));
    }

    /// <summary>
    /// Get employee by ID (Admin, Manager, or Self).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        return Ok(ApiResponse<EmployeeResponseDto>.SuccessResponse(employee));
    }

    /// <summary>
    /// Create a new employee (Admin only).
    /// </summary>
    [HttpPost]
    // [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto request)
    {
        var employee = await _employeeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id },
            ApiResponse<EmployeeResponseDto>.SuccessResponse(employee, "Employee created successfully"));
    }

    /// <summary>
    /// Update an existing employee (Admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    // [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto request)
    {
        var employee = await _employeeService.UpdateAsync(id, request);
        return Ok(ApiResponse<EmployeeResponseDto>.SuccessResponse(employee, "Employee updated successfully"));
    }

    /// <summary>
    /// Delete an employee (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    // [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _employeeService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Employee deleted successfully"));
    }
}
