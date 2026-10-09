using System.Net;
using System.Text.Json;
using EmployeeManagement.Api.Helpers;

namespace EmployeeManagement.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a standardized ApiResponse error.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthException ex)
        {
            // Expected client errors (bad password, expired refresh token…) — no stack trace needed
            _logger.LogInformation("Auth failure {ErrorCode}: {Message}", ex.ErrorCode, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errorCode) = exception switch
        {
            AuthException auth => ((HttpStatusCode)auth.StatusCode, auth.Message, auth.ErrorCode),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message, "NOT_FOUND"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message, AuthErrorCodes.TokenInvalid),
            InvalidOperationException => (HttpStatusCode.Conflict, exception.Message, "CONFLICT"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message, "BAD_REQUEST"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", "INTERNAL_ERROR")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailResponse(message, errorCode: errorCode);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);

        await context.Response.WriteAsync(json);
    }
}
