# Employee Management System (EMS) — Backend API

## Project Overview

Enterprise Employee Management System backend built with **ASP.NET Core 9 Web API** and **PostgreSQL**. This API serves as the backbone for a web (React) and mobile (React Native/Flutter) frontend, covering employee identity, attendance tracking, and leave management.

---

## Philosophy

1. **Clean Architecture** — Strict separation of concerns. Business logic never leaks into controllers or data access. The domain layer has zero external dependencies.
2. **Convention over Configuration** — Follow ASP.NET Core conventions. If the framework provides a pattern (DI, middleware pipeline, configuration binding), use it.
3. **Fail Fast, Fail Loud** — Validate at the boundary (DTOs + FluentValidation). Never let invalid data reach the domain. Use global exception handling to return consistent error responses.
4. **Security First** — JWT + RBAC baked in from day one. Passwords hashed with BCrypt. No secrets in source control.
5. **Async All The Way** — Every I/O-bound operation must be `async/await`. Never `.Result` or `.Wait()`.
6. **Explicit over Implicit** — DTOs for all request/response shapes. No anonymous types crossing API boundaries. Map entities ↔ DTOs explicitly.

---

## Technical Stack

| Concern              | Technology                                      |
|----------------------|-------------------------------------------------|
| Framework            | .NET 9 / ASP.NET Core Web API                   |
| Database             | PostgreSQL                                       |
| ORM                  | Entity Framework Core 9 (Npgsql provider)        |
| Authentication       | JWT Bearer Tokens                                |
| Authorization        | Role-Based Access Control (Admin, Manager, Employee) |
| Password Hashing     | BCrypt.Net                                       |
| Validation           | FluentValidation                                 |
| API Documentation    | Scalar (OpenAPI)                                 |
| Logging              | Built-in ILogger + Serilog (structured)          |

---

## Folder Structure

```
EmployeeManagement.Api/
│
├── .agents/                    # Agent configuration (AGENTS.md)
├── Properties/
│   └── launchSettings.json
│
├── Data/                       # EF Core DbContext & configurations
│   ├── AppDbContext.cs
│   └── Configurations/         # IEntityTypeConfiguration<T> classes
│       ├── EmployeeConfiguration.cs
│       ├── DepartmentConfiguration.cs
│       ├── AttendanceLogConfiguration.cs
│       └── LeaveRequestConfiguration.cs
│
├── Entities/                   # Domain entities (POCO, no EF attributes)
│   ├── Employee.cs
│   ├── Department.cs
│   ├── AttendanceLog.cs
│   └── LeaveRequest.cs
│
├── Enums/                      # Shared enumerations
│   ├── UserRole.cs
│   ├── AttendanceStatus.cs
│   ├── DeviceType.cs
│   ├── LeaveType.cs
│   └── LeaveStatus.cs
│
├── DTOs/                       # Data Transfer Objects (request/response)
│   ├── Auth/
│   │   ├── LoginRequestDto.cs
│   │   ├── LoginResponseDto.cs
│   │   └── RegisterRequestDto.cs
│   ├── Employee/
│   │   ├── EmployeeResponseDto.cs
│   │   ├── CreateEmployeeDto.cs
│   │   └── UpdateEmployeeDto.cs
│   ├── Department/
│   │   ├── DepartmentResponseDto.cs
│   │   ├── CreateDepartmentDto.cs
│   │   └── UpdateDepartmentDto.cs
│   ├── Attendance/
│   │   ├── AttendanceResponseDto.cs
│   │   ├── CheckInDto.cs
│   │   └── CheckOutDto.cs
│   └── Leave/
│       ├── LeaveRequestResponseDto.cs
│       ├── CreateLeaveRequestDto.cs
│       └── ApproveRejectLeaveDto.cs
│
├── Validators/                 # FluentValidation validators
│   ├── Auth/
│   ├── Employee/
│   ├── Department/
│   ├── Attendance/
│   └── Leave/
│
├── Services/                   # Business logic / service layer
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IEmployeeService.cs
│   │   ├── IDepartmentService.cs
│   │   ├── IAttendanceService.cs
│   │   └── ILeaveService.cs
│   ├── AuthService.cs
│   ├── EmployeeService.cs
│   ├── DepartmentService.cs
│   ├── AttendanceService.cs
│   └── LeaveService.cs
│
├── Controllers/                # API controllers (thin — delegate to services)
│   ├── AuthController.cs
│   ├── EmployeesController.cs
│   ├── DepartmentsController.cs
│   ├── AttendanceController.cs
│   └── LeaveRequestsController.cs
│
├── Middleware/                  # Custom middleware
│   └── GlobalExceptionMiddleware.cs
│
├── Helpers/                    # Utility / helper classes
│   ├── ApiResponse.cs          # Standardized JSON response wrapper
│   └── JwtHelper.cs            # Token generation & validation helpers
│
├── Extensions/                 # IServiceCollection extension methods
│   ├── ServiceExtensions.cs
│   └── AuthExtensions.cs
│
├── Migrations/                 # EF Core auto-generated migrations
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs                  # Composition root
├── EmployeeManagement.Api.csproj
└── README.md
```

---

## Coding Standards & Best Practices

### Naming Conventions
- **PascalCase** for classes, methods, properties, enums.
- **camelCase** for local variables, method parameters.
- **_camelCase** for private fields (with underscore prefix).
- **I-prefix** for interfaces (`IAuthService`).
- Controllers: plural nouns (`EmployeesController`, `DepartmentsController`).
- DTOs: suffix with `Dto` (`CreateEmployeeDto`, `EmployeeResponseDto`).

### Controller Rules
- Controllers are **thin**. They accept a request, call a service, and return a response.
- Always return `ApiResponse<T>` for consistency.
- Use `[Authorize(Roles = "...")]` attribute for RBAC.
- Use `[ProducesResponseType]` attributes for OpenAPI documentation.
- Route pattern: `api/[controller]`.

### Service Layer Rules
- All public methods return `Task<T>` (async).
- Services receive DTOs, never raw entities from controllers.
- Services handle business validation (beyond DTO-level validation).
- Inject `AppDbContext` directly (no generic repository pattern — EF Core IS the repository + unit of work).

### Entity Rules
- POCO classes only — no data annotations for EF mapping.
- All EF configuration via Fluent API in `Data/Configurations/`.
- Navigation properties are nullable where optional.
- Use `DateTimeOffset` over `DateTime` for timestamps.

### DTO Rules
- Separate request DTOs from response DTOs (never reuse).
- Response DTOs never expose `PasswordHash` or internal IDs that aren't needed.
- Request DTOs are validated via FluentValidation before reaching the service.

### Error Handling
- Global exception middleware catches all unhandled exceptions.
- Business rule violations throw custom exceptions (e.g., `NotFoundException`, `ConflictException`).
- All API responses use `ApiResponse<T>` wrapper:
  ```json
  {
    "success": true,
    "message": "Employee created successfully",
    "data": { ... },
    "errors": []
  }
  ```

### Security Rules
- **Never** store plain-text passwords.
- JWT tokens include: `sub` (employee ID), `email`, `role` claims.
- Token expiration: configurable via `appsettings.json`.
- CORS: configured for frontend origins only.
- All endpoints except login/register require `[Authorize]`.

### Database Rules
- Use **migrations** for all schema changes — never edit the database manually.
- Seed default data (Admin, Manager, Employee accounts) via migration or `DbContext.OnModelCreating`.
- Every foreign key must have an explicit navigation property.
- Add indexes on frequently queried columns (`EmployeeCode`, `Email`, `WorkDate`).

### Git Practices
- Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `chore:`.
- Never commit `appsettings.Development.json` with real secrets.
- `.gitignore` already covers build artifacts and user-specific files.

---

## Roles & Permissions Matrix

| Endpoint                     | Admin | Manager | Employee |
|------------------------------|:-----:|:-------:|:--------:|
| POST /api/auth/login         |  ✅   |   ✅    |    ✅    |
| POST /api/auth/register      |  ✅   |   ❌    |    ❌    |
| GET /api/employees           |  ✅   |   ✅    |    ❌    |
| GET /api/employees/{id}      |  ✅   |   ✅    |  Self ✅ |
| POST /api/employees          |  ✅   |   ❌    |    ❌    |
| PUT /api/employees/{id}      |  ✅   |   ❌    |    ❌    |
| DELETE /api/employees/{id}   |  ✅   |   ❌    |    ❌    |
| CRUD /api/departments        |  ✅   |   ❌    |    ❌    |
| POST /api/attendance/check-in|  ✅   |   ✅    |    ✅    |
| PUT /api/attendance/check-out|  ✅   |   ✅    |    ✅    |
| GET /api/attendance          |  ✅   |   ✅    |  Self ✅ |
| POST /api/leave-requests     |  ✅   |   ✅    |    ✅    |
| PUT /api/leave-requests/{id}/approve | ✅ | ✅ |    ❌    |
| GET /api/leave-requests      |  ✅   |   ✅    |  Self ✅ |

---

## Configuration Keys (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ems_db;Username=postgres;Password=..."
  },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "EmployeeManagement.Api",
    "Audience": "EmployeeManagement.Client",
    "ExpirationInMinutes": 60
  }
}
```

---

## Default Seed Accounts

| Role     | Email               | Password    |
|----------|---------------------|-------------|
| Admin    | admin@ems.com       | Admin@123   |
| Manager  | manager@ems.com     | Manager@123 |
| Employee | employee@ems.com    | Employee@123|
