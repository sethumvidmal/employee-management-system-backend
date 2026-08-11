using EmployeeManagement.Api.DTOs.Auth;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticate a user and return a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login successful"));
    }

    /// <summary>
    /// Register a new employee (Admin only).
    /// </summary>
    [HttpPost("register")]
    // [Authorize(Roles = "Admin")]  // TODO: Uncomment after JWT is wired up
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), ApiResponse<LoginResponseDto>.SuccessResponse(result, "Registration successful"));
    }
}
