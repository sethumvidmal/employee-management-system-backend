using EmployeeManagement.Api.DTOs.Attendance;
using FluentValidation;

namespace EmployeeManagement.Api.Validators.Attendance;

public class CheckInValidator : AbstractValidator<CheckInDto>
{
    public CheckInValidator()
    {
        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Device type is required")
            .Must(dt => dt is "Web" or "Mobile")
            .WithMessage("Device type must be 'Web' or 'Mobile'");
    }
}
