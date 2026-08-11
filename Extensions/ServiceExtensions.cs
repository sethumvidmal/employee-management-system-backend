using EmployeeManagement.Api.Services;
using EmployeeManagement.Api.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace EmployeeManagement.Api.Extensions;

/// <summary>
/// Registers application services into the DI container.
/// </summary>
public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Business services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveService, LeaveService>();

        // FluentValidation — auto-discover all validators in this assembly
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
