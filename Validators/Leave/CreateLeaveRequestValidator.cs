using EmployeeManagement.Api.DTOs.Leave;
using FluentValidation;

namespace EmployeeManagement.Api.Validators.Leave;

public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveType)
            .NotEmpty().WithMessage("Leave type is required")
            .Must(type => type is "Annual" or "Sick" or "Casual" or "Maternity" or "Paternity" or "Unpaid")
            .WithMessage("Invalid leave type");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
