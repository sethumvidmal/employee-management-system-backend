# Employee Management System API — Full Documentation

Complete reference for the EMS backend: every endpoint, sample requests and responses, and an explanation of the logic behind each operation.

- **Stack:** ASP.NET Core 9, EF Core 9 (Npgsql/PostgreSQL), JWT Bearer + BCrypt, FluentValidation, Scalar (OpenAPI)
- **Base URL (dev):** `http://localhost:5202` (HTTPS profile: `https://localhost:7179`)
- **Interactive docs:** `http://localhost:5202/scalar/v1` *(Development environment only)*
- **Content type:** `application/json`

---

## Table of Contents

1. [Core Concepts](#1-core-concepts)
2. [Authentication & Authorization](#2-authentication--authorization)
3. [Error Handling](#3-error-handling)
4. [Auth Endpoints](#4-auth-endpoints-apiauth)
5. [Employee Endpoints](#5-employee-endpoints-apiemployees)
6. [Department Endpoints](#6-department-endpoints-apidepartments)
7. [Attendance Endpoints](#7-attendance-endpoints-apiattendance)
8. [Leave Request Endpoints](#8-leave-request-endpoints-apileave-requests)
9. [Enumerations](#9-enumerations)
10. [Validation Rules](#10-validation-rules)
11. [Data Model](#11-data-model)
12. [Setup & Configuration](#12-setup--configuration)
13. [End-to-End Walkthrough](#13-end-to-end-walkthrough)

---

## 1. Core Concepts

### 1.1 The `ApiResponse<T>` envelope

Every controller response — success or failure — is wrapped in a single, predictable envelope defined in [Helpers/ApiResponse.cs](Helpers/ApiResponse.cs). Clients can therefore always read `success` first and branch, rather than guessing the shape per endpoint.

```jsonc
{
  "success": true,          // false on any handled error
  "message": "Login successful",
  "data": { },              // payload (object, array, or boolean); omitted when null
  "errors": []              // list of error strings; empty on success
}
```

### 1.2 JSON serialization conventions

Configured in [Program.cs:25-30](Program.cs#L25-L30):

| Rule | Effect |
|---|---|
| `PropertyNamingPolicy = CamelCase` | C# `CheckOutTime` → JSON `checkOutTime` |
| `DefaultIgnoreCondition = WhenWritingNull` | **Null properties are omitted entirely** — an employee with no manager simply has no `manager`/`managerId` keys |
| `DateTimeOffset` | ISO-8601 with offset: `"2026-08-13T09:14:22.4831+00:00"` |
| `DateOnly` | `"2026-08-20"` |
| Enums | Serialized as **strings** (`"Approved"`, `"Present"`) because services call `.ToString()` when mapping to DTOs |

> Because nulls are dropped, the sample responses below only show keys that are actually present. Treat any missing optional key as `null`.

### 1.3 Time and timezone handling

The API draws a hard line between two kinds of temporal value, because they need opposite treatment:

| Kind | Examples | Stored as | Resolved in |
|---|---|---|---|
| **Instant** — a precise moment | `checkInTime`, `checkOutTime`, `createdAt`, `updatedAt`, `appliedOn`, `reviewedAt`, token expiries | `DateTimeOffset`, always **UTC** | UTC — an instant is timezone-independent |
| **Calendar date** — a day on a wall calendar | `workDate`, leave `startDate` / `endDate` | `DateOnly` | The **organisation's local timezone** |

"Today" is a local concept, not a UTC one. At 05:00 in Asia/Colombo (UTC+5:30) the UTC date is still *yesterday*, so deriving a work date from `DateTime.UtcNow` would file an early-morning check-in under the previous day — and then reject the employee's genuine check-in as a duplicate. Attendance therefore resolves dates through [`WorkClock`](Helpers/WorkClock.cs), which converts the current instant into the configured zone:

```csharp
var checkInTime = _clock.UtcNow;              // instant → stored in UTC
var today       = _clock.ToWorkDate(checkInTime); // same instant → local calendar date
```

Because both values come from a single instant, the stored timestamp and the work date can never disagree across a midnight boundary.

The zone is set by **`WorkTime:TimeZoneId`** in [appsettings.json](appsettings.json) and defaults to `Asia/Colombo`. It accepts IANA IDs (`Asia/Colombo`, `Europe/London`, `UTC`) on every platform, and Windows IDs as well. An unrecognised ID does not crash startup — it logs an error and falls back to UTC, so check the startup log line `Work timezone resolved: …` to confirm what is actually in effect.

Leave `startDate` and `endDate` are supplied by the client as plain `yyyy-MM-dd` values and stored verbatim, so they carry no timezone conversion at all.

### 1.4 Architecture & request flow

```
HTTP Request
   │
   ▼
GlobalExceptionMiddleware ──► catches any unhandled exception, maps it to a status
   │                          code and returns an ApiResponse failure
   ▼
CORS ──► Authentication (JWT validation) ──► Authorization ([Authorize] / Roles)
   │
   ▼
Controller ──► reads identity from JWT claims (User.GetEmployeeId(), User.GetRole())
   │           applies row-level rules (e.g. "Employees see only their own records")
   ▼
Service (AuthService, EmployeeService, …) ──► business rules, throws typed exceptions
   │                                          maps entities to response DTOs
   ▼
AppDbContext (EF Core) ──► PostgreSQL
```

The layering is deliberate: controllers stay thin (identity + HTTP shape), services own all business rules and throw typed exceptions, and the middleware translates those exceptions into HTTP status codes exactly once, in one place.

---

## 2. Authentication & Authorization

### 2.1 Token model

Authentication is stateless JWT with a database-backed refresh token, implemented in [Services/AuthService.cs](Services/AuthService.cs) and [Helpers/JwtHelper.cs](Helpers/JwtHelper.cs).

| Token | Lifetime | Storage | Purpose |
|---|---|---|---|
| **Access token** (JWT) | `JwtSettings:ExpirationInMinutes` (default **60 min**) | Client only — never persisted server-side | Sent on every request |
| **Refresh token** | **7 days** (`RefreshTokenExpiryDays`) | `RefreshTokens` table | Exchanges for a fresh access token without re-entering a password |

The access token carries these claims:

| Claim | Value | Used for |
|---|---|---|
| `sub` | Employee `Id` (GUID) | `User.GetEmployeeId()` — who is calling |
| `email` | Employee email | `User.GetEmail()` |
| `jti` | Random GUID | Token uniqueness |
| `role` (`ClaimTypes.Role`) | `Admin` / `Manager` / `Employee` | `[Authorize(Roles = …)]` and `User.GetRole()` |
| `fullName` | `"First Last"` | Display convenience |

Validation ([Extensions/AuthExtensions.cs](Extensions/AuthExtensions.cs)) checks issuer, audience, lifetime and signature with **`ClockSkew = TimeSpan.Zero`** — there is no grace period, so a token is rejected the moment it expires.

### 2.2 Sending the token

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 2.3 Refresh token rotation

`POST /api/auth/refresh` is a **rotating** exchange: the presented token is stamped `RevokedAt = now` and a brand-new 64-byte cryptographically random token is issued alongside the new access token. A refresh token is therefore single-use — replaying one returns `401`. This limits the damage window if a refresh token leaks.

### 2.4 Role matrix

| Endpoint | Anonymous | Employee | Manager | Admin |
|---|:--:|:--:|:--:|:--:|
| `POST /api/auth/login` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/auth/refresh` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/auth/register` | ❌ | ❌ | ❌ | ✅ |
| `GET /api/employees` | ❌ | ❌ | ✅ | ✅ |
| `GET /api/employees/{id}` | ❌ | own only | ✅ | ✅ |
| `POST/PUT/DELETE /api/employees` | ❌ | ❌ | ❌ | ✅ |
| All `/api/departments` | ❌ | ❌ | ❌ | ✅ |
| `POST /api/attendance/check-in` | ❌ | ✅ | ✅ | ✅ |
| `PUT /api/attendance/check-out` | ❌ | ✅ | ✅ | ✅ |
| `GET /api/attendance` | ❌ | own only | ✅ (all) | ✅ (all) |
| `POST /api/leave-requests` | ❌ | ✅ | ✅ | ✅ |
| `GET /api/leave-requests` | ❌ | own only | ✅ (all) | ✅ (all) |
| `GET /api/leave-requests/{id}` | ❌ | ✅ | ✅ | ✅ |
| `PUT /api/leave-requests/{id}/approve` | ❌ | ❌ | ✅ | ✅ |

**Two enforcement styles are in play.** Role gates are declarative (`[Authorize(Roles = "Admin")]`) and reject with `403` before the action runs. Row-level ownership ("employees see only their own data") is enforced *inside* the action: for list endpoints the controller silently overwrites the `employeeId` filter with the caller's own ID, so an Employee asking for someone else's records receives their own list rather than an error.

> **Note on `GET /api/leave-requests/{id}`:** unlike the list endpoint, the by-ID lookup applies no ownership check — any authenticated user who knows a leave request's GUID can read it.

### 2.5 Seed accounts

Seeded via `HasData` in [Data/DbSeeder.cs](Data/DbSeeder.cs) with deterministic GUIDs, so they exist as soon as migrations are applied.

| Role | Email | Password | Employee ID |
|---|---|---|---|
| Admin | `admin@ems.com` | `Admin@123` | `d4e5f6a7-b8c9-0123-def0-1234567890ab` |
| Manager | `manager@ems.com` | `Manager@123` | `e5f6a7b8-c9d0-1234-ef01-234567890abc` |
| Employee | `employee@ems.com` | `Employee@123` | `f6a7b8c9-d0e1-2345-f012-34567890abcd` |

Seeded departments: **IT** (`a1b2c3d4-e5f6-7890-abcd-ef1234567890`), **Human Resources** (`b2c3d4e5-f6a7-8901-bcde-f12345678901`), **Finance** (`c3d4e5f6-a7b8-9012-cdef-123456789012`).

---

## 3. Error Handling

### 3.1 Exception → status code mapping

[Middleware/GlobalExceptionMiddleware.cs](Middleware/GlobalExceptionMiddleware.cs) is registered first in the pipeline and converts typed exceptions thrown by services into HTTP responses. Services never touch `HttpContext`; they just throw the semantically right exception.

| Exception thrown by a service | HTTP status | Meaning in this codebase |
|---|---|---|
| `KeyNotFoundException` | **404** Not Found | Referenced record does not exist (or is inactive) |
| `UnauthorizedAccessException` | **401** Unauthorized | Bad credentials, or an invalid/expired/revoked token |
| `InvalidOperationException` | **409** Conflict | Business-rule conflict — duplicate email, double check-in, overlapping leave |
| `ArgumentException` | **400** Bad Request | Unparseable value — invalid role, leave type, device type |
| *anything else* | **500** Internal Server Error | Message replaced with a generic string; details go to the log only |

> `409 Conflict` is the one to watch: several endpoints document `400` in their `[ProducesResponseType]` attributes, but a duplicate-email or already-checked-in condition actually surfaces as **409** because the service throws `InvalidOperationException`.

Sample handled error:

```json
{
  "success": false,
  "message": "An employee with email 'jane.doe@ems.com' already exists",
  "errors": []
}
```

### 3.2 Validation failures (different shape)

FluentValidation runs via `AddFluentValidationAutoValidation()` **before** the action executes. Because the controllers are `[ApiController]`, a failed validation short-circuits into ASP.NET Core's standard `ValidationProblemDetails` — which is *not* the `ApiResponse` envelope:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Password": [
      "Password must contain at least one uppercase letter",
      "Password must contain at least one special character"
    ],
    "Email": [ "A valid email address is required" ]
  }
}
```

**Client rule of thumb:** on a `400`, check for an `errors` *object* (validation) versus `success: false` (business rule).

### 3.3 Auth failures from the framework

| Situation | Status | Body |
|---|---|---|
| No / malformed / expired `Authorization` header | `401` | Empty body, `WWW-Authenticate: Bearer` header |
| Valid token but wrong role | `403` | Empty body |
| Employee requesting another employee's profile | `403` | `ApiResponse` failure (explicitly returned by the controller) |

---

## 4. Auth Endpoints (`/api/auth`)

Source: [Controllers/AuthController.cs](Controllers/AuthController.cs) · [Services/AuthService.cs](Services/AuthService.cs)

### 4.1 `POST /api/auth/login` — Public

**Logic.** Looks up an employee by email **that is also `IsActive`**, then verifies the password with `BCrypt.Verify` against the stored hash. Both a missing account and a wrong password throw the *same* `UnauthorizedAccessException("Invalid email or password")` — deliberate, so the response cannot be used to enumerate valid emails. On success it mints an access token and persists a new refresh token row. Soft-deleted employees can never log in, because `IsActive` is part of the lookup.

**Request**

```http
POST /api/auth/login
Content-Type: application/json
```
```json
{
  "email": "admin@ems.com",
  "password": "Admin@123"
}
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkNGU1ZjZhNy1iOGM5LTAxMjMtZGVmMC0xMjM0NTY3ODkwYWIiLCJlbWFpbCI6ImFkbWluQGVtcy5jb20iLCJyb2xlIjoiQWRtaW4ifQ.4Xw1s0P9m2QhTt7yZ8pKcNq3dVrJ6bLxAe0FgHiMnUo",
    "refreshToken": "hT8vQ2mZ9pLk3Rw6Yb1Nc5Xd7Fg0JhKl4Ms8Tu2Vw6Yz9Ab3Cd5Ef7Gh1Ij4Kl6Mn8Op0Qr2St4Uv6Wx8Yz0Ab2Cd4Ef6Gh8==",
    "email": "admin@ems.com",
    "fullName": "System Admin",
    "role": "Admin",
    "expiresAt": "2026-08-13T10:14:22.4831Z"
  },
  "errors": []
}
```

**Response `401 Unauthorized`**

```json
{
  "success": false,
  "message": "Invalid email or password",
  "errors": []
}
```

| Status | Cause |
|---|---|
| `200` | Credentials valid |
| `400` | Validation — missing email, malformed email, password shorter than 6 chars |
| `401` | Unknown email, inactive account, or wrong password |

---

### 4.2 `POST /api/auth/register` — Admin only

**Logic.** Admin-gated account creation that returns tokens for the *newly created* employee (not for the admin). Order of checks: unique email → department exists → manager exists (if supplied) → role parses to the `UserRole` enum. The employee code is generated as `EMP` + zero-padded `(total employee count + 1)`, e.g. `EMP004`. The password is hashed with BCrypt (work factor 11) before insert — plaintext is never stored.

> Because the code is derived from a row **count**, deleting rows outright could produce a duplicate code. Deletion in this API is soft (`IsActive = false`) and keeps the row, so counts keep increasing under normal use.

**Request**

```http
POST /api/auth/register
Authorization: Bearer <admin access token>
Content-Type: application/json
```
```json
{
  "firstName": "Nimal",
  "lastName": "Perera",
  "email": "nimal.perera@ems.com",
  "password": "Nimal@123",
  "phoneNumber": "+94771234570",
  "departmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "managerId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
  "role": "Employee"
}
```

**Response `201 Created`**

```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "Kp3Nw8Vz1Bd6Fh9Jm2Qs5Ux7Yc0Ae4Gi8Lo1Rt3Wv6Zb9De2Hj5Kn7Pq0Su3Xy6Ac9Ef1Hk4Mo7Rs0Vw3Zd6Fg9Ij2==",
    "email": "nimal.perera@ems.com",
    "fullName": "Nimal Perera",
    "role": "Employee",
    "expiresAt": "2026-08-13T10:31:07.1194Z"
  },
  "errors": []
}
```

| Status | Cause |
|---|---|
| `201` | Employee created |
| `400` | Validation failure, or `role` not one of `Admin`/`Manager`/`Employee` |
| `401` / `403` | Missing token / caller is not an Admin |
| `404` | `departmentId` or `managerId` does not exist |
| `409` | Email already registered |

---

### 4.3 `POST /api/auth/refresh` — Public

**Logic.** Looks the token up in the `RefreshTokens` table with its owning employee, then rejects it if it is unknown, expired, revoked, or if the owner has been deactivated. Passing all four checks, the old token is revoked and a *new* access + refresh pair is issued (rotation). Note this endpoint is anonymous by design — the refresh token itself is the credential, so an expired access token is not a blocker.

**Request**

```json
{
  "refreshToken": "hT8vQ2mZ9pLk3Rw6Yb1Nc5Xd7Fg0JhKl4Ms8Tu2Vw6Yz9Ab3Cd5Ef7Gh1Ij4Kl6Mn8Op0Qr2St4Uv6Wx8Yz0Ab2Cd4Ef6Gh8=="
}
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...<new>",
    "refreshToken": "Qw2Er4Ty6Ui8Op0As2Df4Gh6Jk8Lz0Xc2Vb4Nm6Qw8Er0Ty2Ui4Op6As8Df0Gh2Jk4Lz6Xc8Vb0Nm2Qw4Er6Ty8Ui0==",
    "email": "admin@ems.com",
    "fullName": "System Admin",
    "role": "Admin",
    "expiresAt": "2026-08-13T11:14:22.4831Z"
  },
  "errors": []
}
```

**Response `401 Unauthorized`** — one of:

```json
{ "success": false, "message": "Invalid refresh token", "errors": [] }
{ "success": false, "message": "Refresh token has expired. Please login again", "errors": [] }
{ "success": false, "message": "Refresh token has been revoked", "errors": [] }
{ "success": false, "message": "Account is deactivated", "errors": [] }
```

---

## 5. Employee Endpoints (`/api/employees`)

Source: [Controllers/EmployeesController.cs](Controllers/EmployeesController.cs) · [Services/EmployeeService.cs](Services/EmployeeService.cs)

The controller carries a class-level `[Authorize]`, so every route below needs a valid token.

### 5.1 `GET /api/employees` — Admin, Manager

**Logic.** Projects employees straight into `EmployeeResponseDto` inside the LINQ query, so EF Core builds a single SQL `SELECT` with joins to department and manager rather than loading full entities. Returns **all** employees, active and inactive — filter on `isActive` client-side if you only want current staff.

**Request**

```http
GET /api/employees
Authorization: Bearer <admin or manager token>
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": [
    {
      "id": "d4e5f6a7-b8c9-0123-def0-1234567890ab",
      "employeeCode": "EMP001",
      "firstName": "System",
      "lastName": "Admin",
      "email": "admin@ems.com",
      "phoneNumber": "+94771234567",
      "department": "IT",
      "departmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "role": "Admin",
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00+00:00"
    },
    {
      "id": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
      "employeeCode": "EMP002",
      "firstName": "John",
      "lastName": "Manager",
      "email": "manager@ems.com",
      "phoneNumber": "+94771234568",
      "department": "IT",
      "departmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "role": "Manager",
      "manager": "System Admin",
      "managerId": "d4e5f6a7-b8c9-0123-def0-1234567890ab",
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00+00:00"
    }
  ],
  "errors": []
}
```

> `EMP001` has no `manager`/`managerId` keys at all — that is the null-omission rule from §1.2, not a missing field.

---

### 5.2 `GET /api/employees/{id}` — Admin, Manager, or self

**Logic.** The route is open to any authenticated user, so the controller adds the ownership rule itself: if the caller's role is `Employee` and the requested `id` is not their own `sub` claim, it returns `403` before hitting the database. Admins and Managers pass through to any record.

**Request**

```http
GET /api/employees/f6a7b8c9-d0e1-2345-f012-34567890abcd
Authorization: Bearer <token>
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": {
    "id": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
    "employeeCode": "EMP003",
    "firstName": "Jane",
    "lastName": "Employee",
    "email": "employee@ems.com",
    "phoneNumber": "+94771234569",
    "department": "IT",
    "departmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "role": "Employee",
    "manager": "John Manager",
    "managerId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
    "isActive": true,
    "createdAt": "2025-01-01T00:00:00+00:00"
  },
  "errors": []
}
```

**Response `403 Forbidden`** (Employee requesting somebody else)

```json
{
  "success": false,
  "message": "You can only view your own profile",
  "errors": []
}
```

**Response `404 Not Found`**

```json
{
  "success": false,
  "message": "Employee with ID '11111111-2222-3333-4444-555555555555' not found",
  "errors": []
}
```

---

### 5.3 `POST /api/employees` — Admin only

**Logic.** Functionally close to `/api/auth/register`, with one meaningful difference: the referenced department and manager must be **active**, not merely present (`d.Id == … && d.IsActive`). It hashes the password, saves, then explicitly re-loads the `Department` and `Manager` navigation properties so the response can include their display names. Unlike register, it returns the employee record rather than tokens.

**Request**

```http
POST /api/employees
Authorization: Bearer <admin token>
Content-Type: application/json
```
```json
{
  "firstName": "Kasun",
  "lastName": "Silva",
  "email": "kasun.silva@ems.com",
  "password": "Kasun@123",
  "phoneNumber": "+94771234571",
  "departmentId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "managerId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
  "role": "Employee"
}
```

**Response `201 Created`**

```http
Location: http://localhost:5202/api/Employees/7c9e6679-7425-40de-944b-e07fc1f90ae7
```
```json
{
  "success": true,
  "message": "Employee created successfully",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "employeeCode": "EMP004",
    "firstName": "Kasun",
    "lastName": "Silva",
    "email": "kasun.silva@ems.com",
    "phoneNumber": "+94771234571",
    "department": "Human Resources",
    "departmentId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "role": "Employee",
    "manager": "John Manager",
    "managerId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
    "isActive": true,
    "createdAt": "2026-08-13T09:22:41.7712+00:00"
  },
  "errors": []
}
```

| Status | Cause |
|---|---|
| `201` | Created |
| `400` | Validation failure, or unparseable `role` |
| `403` | Caller is not Admin |
| `404` | Department or manager missing **or inactive** |
| `409` | Email already exists |

---

### 5.4 `PUT /api/employees/{id}` — Admin only

**Logic.** A true partial update: every field on `UpdateEmployeeDto` is nullable, and each is applied only when supplied, so omitting a key leaves the stored value untouched. Guards that fire only when the corresponding field is present:

- **Email** — re-checked for uniqueness against *other* rows, and skipped entirely if unchanged.
- **DepartmentId** — must exist and be active; skipped if unchanged.
- **ManagerId** — must exist and be active, and **cannot equal the employee's own ID** (no self-management cycle).
- **Role** — must parse to `UserRole`.
- **IsActive** — setting `true` is how you reinstate a soft-deleted employee.

`UpdatedAt` is stamped on every successful call, and the navigation properties are reloaded afterwards so the response reflects a changed department or manager.

**Request** — moving Kasun to Finance and promoting to Manager:

```http
PUT /api/employees/7c9e6679-7425-40de-944b-e07fc1f90ae7
Authorization: Bearer <admin token>
Content-Type: application/json
```
```json
{
  "departmentId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "role": "Manager",
  "phoneNumber": "+94771234599"
}
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Employee updated successfully",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "employeeCode": "EMP004",
    "firstName": "Kasun",
    "lastName": "Silva",
    "email": "kasun.silva@ems.com",
    "phoneNumber": "+94771234599",
    "department": "Finance",
    "departmentId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "role": "Manager",
    "manager": "John Manager",
    "managerId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
    "isActive": true,
    "createdAt": "2026-08-13T09:22:41.7712+00:00"
  },
  "errors": []
}
```

| Status | Cause |
|---|---|
| `200` | Updated |
| `400` | Validation failure, or unparseable `role` |
| `404` | Employee, department, or manager not found / inactive |
| `409` | Email taken by another employee, or employee set as their own manager |

> The password is **not** updatable through this endpoint — `UpdateEmployeeDto` has no password field.

---

### 5.5 `DELETE /api/employees/{id}` — Admin only

**Logic.** A **soft delete**: the row is kept and only `IsActive` flips to `false` (plus an `UpdatedAt` stamp). This preserves the employee's attendance and leave history and keeps foreign keys intact. The practical effect is immediate — login filters on `IsActive`, so the account stops working — and it is reversible via `PUT` with `"isActive": true`.

**Request**

```http
DELETE /api/employees/7c9e6679-7425-40de-944b-e07fc1f90ae7
Authorization: Bearer <admin token>
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Employee deleted successfully",
  "data": true,
  "errors": []
}
```

---

## 6. Department Endpoints (`/api/departments`)

Source: [Controllers/DepartmentsController.cs](Controllers/DepartmentsController.cs) · [Services/DepartmentService.cs](Services/DepartmentService.cs)

The whole controller is decorated `[Authorize(Roles = "Admin")]` — **every** department route is Admin-only, reads included. Managers and Employees get `403`.

### 6.1 `GET /api/departments`

**Logic.** Projects each department together with a computed `employeeCount` that counts **active employees only** (`d.Employees.Count(e => e.IsActive)`), so soft-deleted staff don't inflate headcount. The count is computed in SQL as part of the projection.

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "IT",
      "description": "Information Technology Department",
      "isActive": true,
      "employeeCount": 3,
      "createdAt": "2025-01-01T00:00:00+00:00"
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "Human Resources",
      "description": "Human Resources Department",
      "isActive": true,
      "employeeCount": 1,
      "createdAt": "2025-01-01T00:00:00+00:00"
    },
    {
      "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "name": "Finance",
      "description": "Finance & Accounting Department",
      "isActive": true,
      "employeeCount": 0,
      "createdAt": "2025-01-01T00:00:00+00:00"
    }
  ],
  "errors": []
}
```

### 6.2 `GET /api/departments/{id}`

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "IT",
    "description": "Information Technology Department",
    "isActive": true,
    "employeeCount": 3,
    "createdAt": "2025-01-01T00:00:00+00:00"
  },
  "errors": []
}
```

`404` when the ID does not exist: `"Department with ID '…' not found"`.

### 6.3 `POST /api/departments`

**Logic.** Rejects a duplicate name (case-sensitive comparison, enforced against *all* departments including inactive ones), then inserts with `IsActive = true`. `employeeCount` is returned as `0` without querying, since a brand-new department cannot have members.

**Request**

```json
{
  "name": "Operations",
  "description": "Operations & Logistics Department"
}
```

**Response `201 Created`**

```json
{
  "success": true,
  "message": "Department created successfully",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Operations",
    "description": "Operations & Logistics Department",
    "isActive": true,
    "employeeCount": 0,
    "createdAt": "2026-08-13T09:40:12.5521+00:00"
  },
  "errors": []
}
```

`409` when the name is taken: `"A department with name 'Operations' already exists"`.

### 6.4 `PUT /api/departments/{id}`

**Logic.** Partial update over `name`, `description`, and `isActive`. The uniqueness check runs only when the name actually changes, so re-submitting the same name is not treated as a conflict.

**Request**

```json
{
  "description": "Operations, Logistics & Facilities",
  "isActive": true
}
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Department updated successfully",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Operations",
    "description": "Operations, Logistics & Facilities",
    "isActive": true,
    "employeeCount": 0,
    "createdAt": "2026-08-13T09:40:12.5521+00:00"
  },
  "errors": []
}
```

### 6.5 `DELETE /api/departments/{id}`

**Logic.** Soft delete (`IsActive = false`), mirroring employees. Consequence worth knowing: employees already assigned to it keep their `departmentId`, but the department can no longer be selected when creating or updating an employee, because those paths require `IsActive`.

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Department deleted successfully",
  "data": true,
  "errors": []
}
```

---

## 7. Attendance Endpoints (`/api/attendance`)

Source: [Controllers/AttendanceController.cs](Controllers/AttendanceController.cs) · [Services/AttendanceService.cs](Services/AttendanceService.cs)

### 7.1 `POST /api/attendance/check-in` — any authenticated user

**Logic.** The employee ID is **never** taken from the request body — it comes from the JWT `sub` claim, so a user can only check themselves in. The service then verifies the employee is active, and blocks a second check-in for the same `WorkDate`. Records are stamped `Status = Present` and `CheckOutTime = null`.

**Time handling.** `checkInTime` is an absolute instant stored in **UTC**, but `workDate` is a calendar date resolved in the **organisation's local timezone** (`WorkTime:TimeZoneId`, default `Asia/Colombo`) — see [§1.3](#13-time-and-timezone-handling). Both are derived from the same instant, so they can never disagree across a midnight boundary. In the response above, `checkInTime` of `03:32Z` is `09:02` local on the same day, giving `workDate: "2026-08-13"`.

> The duplicate guard keys on the local work date alone, so once you have checked in you cannot open a second session on that date — even after checking out.

**Request**

```http
POST /api/attendance/check-in
Authorization: Bearer <any valid token>
Content-Type: application/json
```
```json
{
  "deviceType": "Web"
}
```

**Response `201 Created`**

```json
{
  "success": true,
  "message": "Checked in successfully",
  "data": {
    "id": "5b8f1c2e-3d4a-4f6b-9c8d-1e2f3a4b5c6d",
    "employeeId": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
    "employeeName": "Jane Employee",
    "checkInTime": "2026-08-13T03:32:18.9042+00:00",
    "workDate": "2026-08-13",
    "status": "Present",
    "deviceType": "Web"
  },
  "errors": []
}
```

> `checkOutTime` is absent because it is still `null`. Keep the returned `id` — it is required to check out.

| Status | Cause |
|---|---|
| `201` | Checked in |
| `400` | `deviceType` missing or not `Web`/`Mobile` |
| `401` | No/invalid token |
| `404` | The token's employee no longer exists or is inactive |
| `409` | `"You have already checked in today"` |

---

### 7.2 `PUT /api/attendance/check-out` — any authenticated user

**Logic.** Takes the `attendanceLogId` returned by check-in and matches it against **both** the log ID and the caller's employee ID — so a user cannot close someone else's record; a mismatched owner produces the same `404` as a nonexistent ID. It then refuses a record that already has a `CheckOutTime`, and otherwise stamps `DateTimeOffset.UtcNow`.

**Request**

```json
{
  "attendanceLogId": "5b8f1c2e-3d4a-4f6b-9c8d-1e2f3a4b5c6d"
}
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Checked out successfully",
  "data": {
    "id": "5b8f1c2e-3d4a-4f6b-9c8d-1e2f3a4b5c6d",
    "employeeId": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
    "employeeName": "Jane Employee",
    "checkInTime": "2026-08-13T03:32:18.9042+00:00",
    "checkOutTime": "2026-08-13T12:05:44.1180+00:00",
    "workDate": "2026-08-13",
    "status": "Present",
    "deviceType": "Web"
  },
  "errors": []
}
```

| Status | Cause |
|---|---|
| `200` | Checked out |
| `404` | `"Attendance log not found or does not belong to you"` |
| `409` | `"You have already checked out for this record"` |

---

### 7.3 `GET /api/attendance` — any authenticated user

**Logic.** A filtered query with four optional parameters, each applied to the `IQueryable` only when supplied, so the SQL contains exactly the predicates you asked for. The security rule sits in the controller: **if the caller's role is `Employee`, the `employeeId` filter is overwritten with their own ID**, silently ignoring whatever they passed. Admins and Managers query freely across everyone. Results are sorted newest-first by `workDate`, then `checkInTime`.

**Query parameters**

| Parameter | Type | Description |
|---|---|---|
| `employeeId` | GUID | Filter by employee — **ignored (forced to self) for the Employee role** |
| `departmentId` | GUID | Filter by the employee's current department |
| `fromDate` | `yyyy-MM-dd` | Inclusive lower bound on `workDate` |
| `toDate` | `yyyy-MM-dd` | Inclusive upper bound on `workDate` |

Both date bounds compare against `workDate`, which is a **local** calendar date ([§1.3](#13-time-and-timezone-handling)) — so `fromDate=2026-08-13&toDate=2026-08-13` returns exactly the shifts worked on the 13th in the organisation's timezone, regardless of where those instants fall in UTC.

**Request**

```http
GET /api/attendance?departmentId=a1b2c3d4-e5f6-7890-abcd-ef1234567890&fromDate=2026-08-01&toDate=2026-08-13
Authorization: Bearer <manager or admin token>
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": [
    {
      "id": "5b8f1c2e-3d4a-4f6b-9c8d-1e2f3a4b5c6d",
      "employeeId": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
      "employeeName": "Jane Employee",
      "checkInTime": "2026-08-13T03:32:18.9042+00:00",
      "checkOutTime": "2026-08-13T12:05:44.1180+00:00",
      "workDate": "2026-08-13",
      "status": "Present",
      "deviceType": "Web"
    },
    {
      "id": "9d2a4b6c-8e0f-4a2b-b4c6-d8e0f2a4b6c8",
      "employeeId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
      "employeeName": "John Manager",
      "checkInTime": "2026-08-12T03:58:02.3310+00:00",
      "checkOutTime": "2026-08-12T13:11:29.7745+00:00",
      "workDate": "2026-08-12",
      "status": "Present",
      "deviceType": "Mobile"
    }
  ],
  "errors": []
}
```

An empty result set returns `200` with `"data": []` — never a `404`.

---

## 8. Leave Request Endpoints (`/api/leave-requests`)

Source: [Controllers/LeaveRequestsController.cs](Controllers/LeaveRequestsController.cs) · [Services/LeaveService.cs](Services/LeaveService.cs)

### 8.1 `POST /api/leave-requests` — any authenticated user

**Logic.** Like check-in, the applicant is the JWT's `sub` — you can only apply for yourself. The service parses `leaveType`, re-checks that `endDate >= startDate` (defence in depth: FluentValidation already checked it), and then runs the interesting rule: an **overlap check** against the applicant's existing requests where `Status != Rejected` and the date ranges intersect (`existing.StartDate <= new.EndDate && existing.EndDate >= new.StartDate`). Pending *and* Approved requests both block, so you cannot stack two claims on the same days; only rejecting the old one frees the dates. New requests always start as `Pending`.

**Request**

```http
POST /api/leave-requests
Authorization: Bearer <any valid token>
Content-Type: application/json
```
```json
{
  "leaveType": "Annual",
  "startDate": "2026-09-01",
  "endDate": "2026-09-05",
  "reason": "Family vacation"
}
```

**Response `201 Created`**

```json
{
  "success": true,
  "message": "Leave request submitted successfully",
  "data": {
    "id": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
    "employeeId": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
    "employeeName": "Jane Employee",
    "leaveType": "Annual",
    "startDate": "2026-09-01",
    "endDate": "2026-09-05",
    "reason": "Family vacation",
    "status": "Pending",
    "appliedOn": "2026-08-13T09:55:30.6018+00:00"
  },
  "errors": []
}
```

| Status | Cause |
|---|---|
| `201` | Submitted |
| `400` | Validation failure, unparseable `leaveType`, or `endDate` before `startDate` |
| `404` | Applicant not found or inactive |
| `409` | `"You already have a leave request for overlapping dates"` |

> The `leaveType` values accepted by the FluentValidation rule are `Annual`, `Sick`, `Casual`, `Maternity`, `Paternity`, `Unpaid`.

---

### 8.2 `PUT /api/leave-requests/{id}/approve` — Admin, Manager

**Logic.** A single endpoint handles both outcomes — the boolean `isApproved` decides between `Approved` and `Rejected`, and the response `message` is worded to match. Two rules protect the decision: the request must still be `Pending` (a second review returns `409`), and **the approver cannot be the applicant**, which stops a Manager or Admin from signing off their own leave. On success it records `ApprovedById` and `ReviewedAt`, then loads the approver's name for the response.

**Request** — approve:

```http
PUT /api/leave-requests/c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f/approve
Authorization: Bearer <manager or admin token>
Content-Type: application/json
```
```json
{
  "isApproved": true
}
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Leave request approved",
  "data": {
    "id": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
    "employeeId": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
    "employeeName": "Jane Employee",
    "leaveType": "Annual",
    "startDate": "2026-09-01",
    "endDate": "2026-09-05",
    "reason": "Family vacation",
    "status": "Approved",
    "approvedBy": "John Manager",
    "appliedOn": "2026-08-13T09:55:30.6018+00:00",
    "reviewedAt": "2026-08-13T10:12:47.2205+00:00"
  },
  "errors": []
}
```

**Request** — reject: same route with `{ "isApproved": false }` → `status: "Rejected"`, message `"Leave request rejected"`.

| Status | Cause |
|---|---|
| `200` | Reviewed |
| `403` | Caller has the `Employee` role |
| `404` | Leave request not found |
| `409` | `"Leave request has already been approved"` / `"…rejected"`, or `"You cannot approve or reject your own leave request"` |

---

### 8.3 `GET /api/leave-requests` — any authenticated user

**Logic.** Same ownership pattern as attendance: an `Employee` caller has `employeeId` forced to their own ID. The `status` filter is parsed leniently — an unrecognised value is **silently ignored** rather than erroring, so `?status=banana` returns unfiltered results. Sorted newest-first by `appliedOn`.

**Query parameters**

| Parameter | Type | Description |
|---|---|---|
| `employeeId` | GUID | Filter by applicant — ignored (forced to self) for the Employee role |
| `status` | string | `Pending` \| `Approved` \| `Rejected` (case-insensitive; invalid values ignored) |

**Request**

```http
GET /api/leave-requests?status=Pending
Authorization: Bearer <manager or admin token>
```

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": [
    {
      "id": "e7f8a9b0-c1d2-4e3f-a4b5-c6d7e8f9a0b1",
      "employeeId": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
      "employeeName": "John Manager",
      "leaveType": "Sick",
      "startDate": "2026-08-20",
      "endDate": "2026-08-21",
      "reason": "Medical appointment",
      "status": "Pending",
      "appliedOn": "2026-08-13T08:10:05.4402+00:00"
    }
  ],
  "errors": []
}
```

### 8.4 `GET /api/leave-requests/{id}` — any authenticated user

**Logic.** Straight lookup including applicant and approver names. **No ownership check is applied here** — any authenticated caller with the GUID can read the record, including its `reason`. Treat leave request IDs as semi-sensitive, or add an ownership guard if that is not acceptable for your deployment.

**Response `200 OK`**

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": {
    "id": "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f",
    "employeeId": "f6a7b8c9-d0e1-2345-f012-34567890abcd",
    "employeeName": "Jane Employee",
    "leaveType": "Annual",
    "startDate": "2026-09-01",
    "endDate": "2026-09-05",
    "reason": "Family vacation",
    "status": "Approved",
    "approvedBy": "John Manager",
    "appliedOn": "2026-08-13T09:55:30.6018+00:00",
    "reviewedAt": "2026-08-13T10:12:47.2205+00:00"
  },
  "errors": []
}
```

`404` → `"Leave request with ID '…' not found"`.

---

## 9. Enumerations

Every enum is persisted as its **name string** in PostgreSQL (`HasConversion<string>()` in [Data/Configurations/](Data/Configurations/)) and exchanged as a string in JSON, so database rows stay readable. Parsing from request bodies is case-insensitive at the service layer (`Enum.TryParse(…, ignoreCase: true)`), but FluentValidation's `Must(…)` rules compare exact strings — so send the exact casing shown below.

| Enum | Values | Used by |
|---|---|---|
| `UserRole` | `Employee`, `Manager`, `Admin` | Employee role, JWT role claim |
| `AttendanceStatus` | `Present`, `Absent`, `Late`, `HalfDay` | Attendance records — the API currently only ever writes `Present` |
| `DeviceType` | `Web`, `Mobile` | Check-in source |
| `LeaveType` | `Annual`, `Sick`, `Casual`, `Maternity`, `Paternity`, `Unpaid` | Leave requests |
| `LeaveStatus` | `Pending`, `Approved`, `Rejected` | Leave request lifecycle |

---

## 10. Validation Rules

FluentValidation validators live in [Validators/](Validators/) and are auto-discovered from the assembly. They run **before** the controller action, and a failure returns `400` with the `ValidationProblemDetails` shape from §3.2.

### Password policy (register & create employee)

| Rule | Message |
|---|---|
| Not empty | `Password is required` |
| Minimum 6 characters | `Password must be at least 6 characters` |
| At least one uppercase `[A-Z]` | `Password must contain at least one uppercase letter` |
| At least one lowercase `[a-z]` | `Password must contain at least one lowercase letter` |
| At least one digit `[0-9]` | `Password must contain at least one digit` |
| At least one special char `[^a-zA-Z0-9]` | `Password must contain at least one special character` |

### Field constraints

| DTO | Field | Rule |
|---|---|---|
| `LoginRequestDto` | `email` | Required, valid email format |
| | `password` | Required, min 6 |
| `RegisterRequestDto` | `firstName` / `lastName` | Required, max 100 |
| | `email` | Required, valid format |
| | `departmentId` | Required (non-empty GUID) |
| | `role` | Required, one of `Admin` / `Manager` / `Employee` |
| `CreateEmployeeDto` | `firstName` / `lastName` | Required, max 100 |
| | `email` | Required, valid format, max 200 |
| | `phoneNumber` | Max 20 (only when supplied) |
| | `departmentId` | Required |
| | `role` | Required, one of the three roles |
| `UpdateEmployeeDto` | all fields | Same limits, but each rule runs **only when the field is non-null** |
| `CreateDepartmentDto` | `name` | Required, max 100 |
| | `description` | Max 500 |
| `UpdateDepartmentDto` | `name` / `description` | Same limits, applied only when supplied |
| `CreateLeaveRequestDto` | `leaveType` | Required, one of the six leave types |
| | `startDate` / `endDate` | Required; `endDate >= startDate` |
| | `reason` | Max 500 |
| `CheckInDto` | `deviceType` | Required, `Web` or `Mobile` |

> `CheckOutDto`, `ApproveRejectLeaveDto`, and `RefreshTokenRequestDto` have no validators — their single fields are checked by the service instead.

---

## 11. Data Model

```
Department 1 ──────< * Employee
                        │  │  │
                        │  │  └──< * RefreshToken
                        │  └─────< * AttendanceLog
                        └────────< * LeaveRequest  >── ApprovedBy (Employee, nullable)
                        │
                        └── Manager (self-reference, nullable) ──< Subordinates
```

| Entity | Key fields | Notes |
|---|---|---|
| **Department** | `Id`, `Name`, `Description`, `IsActive`, `CreatedAt`, `UpdatedAt` | Soft-deleted via `IsActive` |
| **Employee** | `Id`, `EmployeeCode`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `PhoneNumber`, `Role`, `IsActive`, `DepartmentId`, `ManagerId` | `ManagerId` is a self-reference; `PasswordHash` is BCrypt and never leaves the server |
| **AttendanceLog** | `Id`, `CheckInTime`, `CheckOutTime`, `WorkDate`, `Status`, `DeviceType`, `EmployeeId` | One row per employee per `WorkDate` |
| **LeaveRequest** | `Id`, `LeaveType`, `StartDate`, `EndDate`, `Reason`, `Status`, `AppliedOn`, `ReviewedAt`, `EmployeeId`, `ApprovedById` | `ApprovedById` stays null until reviewed |
| **RefreshToken** | `Id`, `Token`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `EmployeeId` | Computed `IsRevoked` / `IsExpired` / `IsActive` are C#-only, not columns |

### Constraints and indexes

Defined in [Data/Configurations/](Data/Configurations/); primary keys default to PostgreSQL's `gen_random_uuid()`.

| Table | Unique | Non-unique index | Delete behaviour |
|---|---|---|---|
| `Employees` | `EmployeeCode`, `Email` | — | → Department: `Restrict` (a department with staff cannot be hard-deleted); → Manager: `SetNull` |
| `Departments` | `Name` | — | — |
| `AttendanceLogs` | — | `WorkDate`, `(EmployeeId, WorkDate)` | → Employee: `Cascade` |
| `LeaveRequests` | — | `Status`, `(EmployeeId, Status)` | → Employee: `Cascade`; → ApprovedBy: `SetNull` |
| `RefreshTokens` | `Token` | `EmployeeId` | → Employee: `Cascade` |

The unique index on `Employees.Email` is the real safety net behind the duplicate-email checks in the services — those checks produce the friendly `409`, while the index guarantees correctness under concurrent inserts.

> **Length mismatch to be aware of:** `Employees.Email` is a `varchar(150)` column, but `CreateEmployeeValidator` allows up to 200 characters. An email between 151 and 200 characters passes validation and then fails at the database, surfacing as a generic `500`. Either tighten the validator to 150 or widen the column.

> `(EmployeeId, WorkDate)` on `AttendanceLogs` is **not** unique — the one-check-in-per-day rule is enforced only in application code, so two simultaneous check-in requests could in principle both succeed.

Migrations: `InitialCreate` and `AddRefreshTokens` in [Migrations/](Migrations/).

---

## 12. Setup & Configuration

### Prerequisites

- .NET 9 SDK
- PostgreSQL 14+ with a database named `ems_db`

### Run

```bash
dotnet restore
dotnet ef database update    # creates schema + seeds departments and the three accounts
dotnet run                   # http://localhost:5202
```

Then open **http://localhost:5202/scalar/v1** for the interactive Scalar UI (Development only — the OpenAPI endpoints are not mapped in other environments).

### Configuration keys

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=ems_db;…` |
| `JwtSettings:SecretKey` | HMAC-SHA256 signing key — **must be at least 32 characters** | placeholder; **replace before deploying** |
| `JwtSettings:Issuer` | `iss` claim | `EmployeeManagement.Api` |
| `JwtSettings:Audience` | `aud` claim | `EmployeeManagement.Client` |
| `JwtSettings:ExpirationInMinutes` | Access token lifetime | `60` |
| `WorkTime:TimeZoneId` | Organisation's local timezone; decides what "today" means for attendance work dates ([§1.3](#13-time-and-timezone-handling)) | `Asia/Colombo` |
| `Cors:AllowedOrigins` | Array of allowed browser origins | `http://localhost:3000`, `http://localhost:5173` |

Put real secrets in `appsettings.Development.json` (gitignored), user-secrets, or environment variables — never in the committed `appsettings.json`.

**Deployment checklist**

- Replace `JwtSettings:SecretKey` with a strong random value ≥ 32 chars.
- Set `RequireHttpsMetadata = true` in [Extensions/AuthExtensions.cs](Extensions/AuthExtensions.cs#L28) — it is currently `false` to allow plain HTTP in development.
- Change the seeded account passwords, which are published in this document and in the repository.
- Set `WorkTime:TimeZoneId` to the organisation's actual timezone and confirm the `Work timezone resolved: …` startup log — a silent fallback to UTC would shift every attendance work date.
- CORS uses `AllowCredentials()` with an explicit origin list, so wildcards will not work — list your real frontend origins.

---

## 13. End-to-End Walkthrough

A complete Employee-role day, using `curl`.

**1 — Log in and capture the token**

```bash
curl -X POST http://localhost:5202/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"employee@ems.com","password":"Employee@123"}'
```

**2 — Check in for the day**

```bash
curl -X POST http://localhost:5202/api/attendance/check-in \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"deviceType":"Web"}'
# → 201, keep data.id as $LOG_ID
```

**3 — Apply for leave**

```bash
curl -X POST http://localhost:5202/api/leave-requests \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"leaveType":"Annual","startDate":"2026-09-01","endDate":"2026-09-05","reason":"Family vacation"}'
# → 201, status "Pending", keep data.id as $LEAVE_ID
```

**4 — Check out**

```bash
curl -X PUT http://localhost:5202/api/attendance/check-out \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"attendanceLogId\":\"$LOG_ID\"}"
```

**5 — Manager approves the leave** (log in as `manager@ems.com` first)

```bash
curl -X PUT http://localhost:5202/api/leave-requests/$LEAVE_ID/approve \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isApproved":true}'
# → 200, status "Approved", approvedBy "John Manager"
```

**6 — Refresh the access token when it expires**

```bash
curl -X POST http://localhost:5202/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
# → 200 with a NEW refresh token; the old one is now revoked
```
