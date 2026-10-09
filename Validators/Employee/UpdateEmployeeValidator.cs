using EmployeeManagement.Api.DTOs.Employee;
using FluentValidation;

namespace EmployeeManagement.Api.Validators.Employee;

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
            .When(x => x.LastName is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters")
            .When(x => x.Email is not null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Role)
            .Must(role => role is "Admin" or "Manager" or "Employee")
            .WithMessage("Role must be Admin, Manager, or Employee")
            .When(x => x.Role is not null);
    }
}
