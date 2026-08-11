using EmployeeManagement.Api.Helpers;

namespace EmployeeManagement.Api.Extensions;

/// <summary>
/// Registers JWT authentication and authorization services.
/// </summary>
public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        services.AddSingleton(jwtSettings);

        // TODO: Wire up JWT Bearer authentication middleware
        // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddJwtBearer(options => { ... });

        services.AddAuthorization();

        return services;
    }
}
