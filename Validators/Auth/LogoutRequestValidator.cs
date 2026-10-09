using EmployeeManagement.Api.DTOs.Auth;
using FluentValidation;

namespace EmployeeManagement.Api.Validators.Auth;

public class LogoutRequestValidator : AbstractValidator<LogoutRequestDto>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
