using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
