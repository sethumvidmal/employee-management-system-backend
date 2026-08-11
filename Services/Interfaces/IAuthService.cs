using EmployeeManagement.Api.DTOs.Auth;

namespace EmployeeManagement.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
}
