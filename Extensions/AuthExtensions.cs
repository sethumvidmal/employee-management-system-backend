using System.Text;
using System.Text.Json;
using EmployeeManagement.Api.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Extensions;

/// <summary>
/// Registers JWT authentication and authorization services.
/// </summary>
public static class AuthExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        // Fail fast at startup instead of on the first login (HS256 needs a key of at least 256 bits)
        if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
        {
            throw new InvalidOperationException(
                $"{JwtSettings.SectionName}:SecretKey must be at least 32 characters long. " +
                "Set it in appsettings or via the JwtSettings__SecretKey environment variable.");
        }

        services.AddSingleton(jwtSettings);
        services.AddSingleton(new JwtHelper(jwtSettings));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // TLS is terminated by the reverse proxy
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero // No tolerance for token expiry
            };

            // Return the standard ApiResponse JSON instead of an empty 401/403 body
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    var (message, errorCode) = context.AuthenticateFailure switch
                    {
                        SecurityTokenExpiredException =>
                            ("Access token has expired. Please refresh your token.", AuthErrorCodes.TokenExpired),
                        not null =>
                            ("Access token is invalid. Please login again.", AuthErrorCodes.TokenInvalid),
                        null =>
                            ("Authentication required. Please provide a valid access token.", AuthErrorCodes.TokenMissing)
                    };

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers.WWWAuthenticate = errorCode == AuthErrorCodes.TokenExpired
                        ? "Bearer error=\"invalid_token\", error_description=\"The token has expired\""
                        : "Bearer";

                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<object>.FailResponse(message, errorCode: errorCode), JsonOptions);
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;

                    await context.Response.WriteAsJsonAsync(
                        ApiResponse<object>.FailResponse(
                            "You do not have permission to access this resource.",
                            errorCode: AuthErrorCodes.Forbidden),
                        JsonOptions);
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
