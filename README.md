# Employee Management System (EMS) — Backend API

Enterprise Employee Management System backend built with **ASP.NET Core 9**, **Entity Framework Core**, and **PostgreSQL**.

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

Update `appsettings.json` with your PostgreSQL credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ems_db;Username=postgres;Password=YOUR_PASSWORD"
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

The API will start at `https://localhost:5001` (or the port defined in `launchSettings.json`).

### 6. Open API documentation

Navigate to the OpenAPI endpoint at `/openapi/v1.json` when running in Development mode.

## Configuration Keys

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `JwtSettings:SecretKey` | Secret key for signing JWT tokens (min 32 chars) |
| `JwtSettings:Issuer` | JWT issuer claim |
| `JwtSettings:Audience` | JWT audience claim |
| `JwtSettings:ExpirationInMinutes` | Token lifetime in minutes |
| `Cors:AllowedOrigins` | Array of allowed frontend origins |

## Default Seed Accounts

| Role     | Email              | Password     |
|----------|--------------------|--------------|
| Admin    | admin@ems.com      | Admin@123    |
| Manager  | manager@ems.com    | Manager@123  |
| Employee | employee@ems.com   | Employee@123 |

## API Modules

- **Auth** — Login, Register (JWT issuance)
- **Employees** — CRUD with hierarchical manager relationships
- **Departments** — CRUD management
- **Attendance** — Check-in/out, queryable history
- **Leave Requests** — Submit, approve/reject workflow

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 9 / ASP.NET Core |
| Database | PostgreSQL |
| ORM | Entity Framework Core 9 |
| Auth | JWT + BCrypt |
| Validation | FluentValidation |
| Docs | OpenAPI / Scalar |
