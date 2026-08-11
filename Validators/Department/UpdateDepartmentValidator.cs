using EmployeeManagement.Api.DTOs.Department;
using FluentValidation;

namespace EmployeeManagement.Api.Validators.Department;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Department name must not exceed 100 characters")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description is not null);
    }
}
