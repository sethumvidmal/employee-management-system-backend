using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.Auth;
using EmployeeManagement.Api.Helpers;
using EmployeeManagement.Api.Services.Interfaces;

namespace EmployeeManagement.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, JwtSettings jwtSettings, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        // TODO: Implement login logic
        throw new NotImplementedException();
    }

    public Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // TODO: Implement register logic
        throw new NotImplementedException();
    }
}
