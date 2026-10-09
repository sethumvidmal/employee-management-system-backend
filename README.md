# Employee Management System (EMS) — Backend API

Enterprise Employee Management System backend built with **ASP.NET Core 9**, **Entity Framework Core 9**, and **MariaDB**.

For production hosting on Ubuntu (with an existing MariaDB server) see **[DEPLOYMENT.md](DEPLOYMENT.md)**.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MariaDB](https://mariadb.org/download/) (10.6+)
- A MariaDB database named `ems_db` (`CREATE DATABASE ems_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`)

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd EmployeeManagement.Api
```

### 2. Configure the database

Create `appsettings.Development.json` (gitignored) with your local credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=ems_db;User=root;Password=YOUR_PASSWORD"
  },
  "Database": {
    "ServerVersion": "10.11.0-mariadb"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
  }
}
```

### 3. Restore packages

```bash
dotnet restore
```

### 4. Apply migrations

```bash
dotnet ef database update
```

### 5. Run the application

```bash
dotnet run
```

The API will start at `http://localhost:5202`.

### 6. Open API Documentation (Scalar UI)

Navigate to **http://localhost:5202/scalar/v1** for interactive API docs with built-in JWT authentication.

---

## Configuration Keys

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | MariaDB connection string |
| `Database:ServerVersion` | MariaDB server version, e.g. `10.11.0-mariadb` (default `10.6.0-mariadb`) |
| `JwtSettings:SecretKey` | Secret key for signing JWT tokens (min 32 chars) |
| `JwtSettings:Issuer` | JWT issuer claim |
| `JwtSettings:Audience` | JWT audience claim |
| `JwtSettings:ExpirationInMinutes` | Access token lifetime in minutes (default: 60) |
| `WorkTime:TimeZoneId` | Organisation's local timezone, e.g. `Asia/Colombo` (default). Determines what "today" means for attendance work dates |
| `Cors:AllowedOrigins` | Array of allowed frontend origins |

### Time handling

Timestamps (`checkInTime`, `createdAt`, `appliedOn`, …) are absolute instants and are always stored in **UTC**. Calendar dates (`workDate`) are wall-calendar values and are resolved in the **organisation's local timezone** via `WorkTime:TimeZoneId`, because "today" is a local concept — at 05:00 in Asia/Colombo (UTC+5:30) the UTC date is still the previous day. An unrecognised timezone ID logs an error and falls back to UTC, so confirm the `Work timezone resolved: …` line in the startup log.

## Default Seed Accounts

| Role     | Email              | Password     |
|----------|--------------------|--------------| 
| Admin    | admin@ems.com      | Admin@123    |
| Manager  | manager@ems.com    | Manager@123  |
| Employee | employee@ems.com   | Employee@123 |

## API Endpoints

### Authentication (`/api/auth`)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/auth/login` | Public | Login and receive access + refresh tokens |
| POST | `/api/auth/register` | Admin | Register a new employee |
| POST | `/api/auth/refresh` | Public | Get new access token using refresh token (rotates the refresh token) |
| POST | `/api/auth/logout` | Public (refresh token in body) | Revoke the current session's refresh token |
| POST | `/api/auth/logout-all` | All Authenticated | Revoke all of the user's refresh tokens (log out everywhere) |

#### Auth responses

Login, register and refresh return:

```json
{
  "success": true,
  "message": "Welcome back, System Admin",
  "data": {
    "employeeId": "d4e5f6a7-b8c9-0123-def0-1234567890ab",
    "employeeCode": "EMP001",
    "email": "admin@ems.com",
    "fullName": "System Admin",
    "role": "Admin",
    "tokenType": "Bearer",
    "accessToken": "eyJhbGciOi...",
    "expiresAt": "2026-10-09T10:00:00Z",
    "expiresIn": 3600,
    "refreshToken": "q1w2e3...",
    "refreshTokenExpiresAt": "2026-10-16T09:00:00Z"
  },
  "errors": []
}
```

Failures use the same envelope with `success: false` and a machine-readable `errorCode`:

| Status | `errorCode` | When | Client action |
|--------|-------------|------|---------------|
| 400 | `VALIDATION_FAILED` | Request body failed validation (details in `errors`) | Show errors |
| 401 | `INVALID_CREDENTIALS` | Wrong email or password | Show error |
| 403 | `ACCOUNT_DEACTIVATED` | Correct password but the account is deactivated | Show error |
| 401 | `TOKEN_MISSING` | No `Authorization: Bearer` header on a protected endpoint | Redirect to login |
| 401 | `TOKEN_EXPIRED` | Access token expired | Call `/api/auth/refresh`, then retry |
| 401 | `TOKEN_INVALID` | Malformed or tampered access token | Redirect to login |
| 403 | `FORBIDDEN` | Authenticated but the role is not allowed | Show "no permission" |
| 401 | `REFRESH_TOKEN_INVALID` / `REFRESH_TOKEN_EXPIRED` / `REFRESH_TOKEN_REVOKED` | Refresh failed | Redirect to login |

Logout is stateless for access tokens. After `/logout`, the client must delete its stored access
token. It remains technically valid until `expiresAt`, but it can no longer be refreshed.

### Employees (`/api/employees`)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/employees` | Admin, Manager | List all employees |
| GET | `/api/employees/{id}` | Admin, Manager, Self | Get employee by ID |
| POST | `/api/employees` | Admin | Create employee |
| PUT | `/api/employees/{id}` | Admin | Update employee |
| DELETE | `/api/employees/{id}` | Admin | Soft-delete employee |

### Departments (`/api/departments`)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/departments` | Admin | List all departments |
| GET | `/api/departments/{id}` | Admin | Get department by ID |
| POST | `/api/departments` | Admin | Create department |
| PUT | `/api/departments/{id}` | Admin | Update department |
| DELETE | `/api/departments/{id}` | Admin | Soft-delete department |

### Attendance (`/api/attendance`)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/attendance/check-in` | All Authenticated | Check in (1 per day) |
| PUT | `/api/attendance/check-out` | All Authenticated | Check out |
| GET | `/api/attendance` | All (Employees see own only) | Query by employee, dept, date range |

### Leave Requests (`/api/leave-requests`)
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/leave-requests` | All Authenticated | Submit leave request |
| PUT | `/api/leave-requests/{id}/approve` | Admin, Manager | Approve or reject |
| GET | `/api/leave-requests` | All (Employees see own only) | List leave requests |
| GET | `/api/leave-requests/{id}` | All Authenticated | Get by ID |

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 9 / ASP.NET Core Web API |
| Database | MariaDB |
| ORM | Entity Framework Core 9 (Pomelo MySQL provider) |
| Auth | JWT Bearer + BCrypt + Refresh Tokens |
| Validation | FluentValidation |
| Docs | Scalar (OpenAPI) |
| API Responses | Standardized `ApiResponse<T>` wrapper |
