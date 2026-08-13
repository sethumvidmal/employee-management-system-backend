# Employee Management System (EMS) — Backend API

Enterprise Employee Management System backend built with **ASP.NET Core 9**, **Entity Framework Core 9**, and **PostgreSQL**.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (v14+)
- A PostgreSQL database named `ems_db`

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
    "DefaultConnection": "Host=localhost;Port=5432;Database=ems_db;Username=postgres;Password=YOUR_PASSWORD"
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
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
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
| POST | `/api/auth/refresh` | Public | Get new access token using refresh token |

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
| Database | PostgreSQL |
| ORM | Entity Framework Core 9 (Npgsql) |
| Auth | JWT Bearer + BCrypt + Refresh Tokens |
| Validation | FluentValidation |
| Docs | Scalar (OpenAPI) |
| API Responses | Standardized `ApiResponse<T>` wrapper |
