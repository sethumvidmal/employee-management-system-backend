using EmployeeManagement.Api.DTOs.Auth;
using FluentValidation;

namespace EmployeeManagement.Api.Validators.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
