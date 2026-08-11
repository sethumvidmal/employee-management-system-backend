using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Extensions;
using EmployeeManagement.Api.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Services
// ──────────────────────────────────────────────

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// Application services (DI registrations)
builder.Services.AddApplicationServices();

// Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ──────────────────────────────────────────────
// Middleware Pipeline
// ──────────────────────────────────────────────

// Global exception handling (first in pipeline)
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// app.UseAuthentication();  // TODO: Uncomment after JWT is wired up
// app.UseAuthorization();   // TODO: Uncomment after JWT is wired up

app.MapControllers();

app.Run();
