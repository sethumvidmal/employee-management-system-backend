using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Data;

/// <summary>
/// Seeds default departments and user accounts (Admin, Manager, Employee)
/// into the database using deterministic GUIDs for repeatability.
/// </summary>
public static class DbSeeder
{
    // Deterministic IDs so seeds are idempotent across migrations
    private static readonly Guid ItDepartmentId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid HrDepartmentId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid FinanceDepartmentId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");

    private static readonly Guid AdminId = Guid.Parse("d4e5f6a7-b8c9-0123-def0-1234567890ab");
    private static readonly Guid ManagerId = Guid.Parse("e5f6a7b8-c9d0-1234-ef01-234567890abc");
    private static readonly Guid EmployeeId = Guid.Parse("f6a7b8c9-d0e1-2345-f012-34567890abcd");

    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedDepartments(modelBuilder);
        SeedEmployees(modelBuilder);
    }

    private static void SeedDepartments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new Department
            {
                Id = ItDepartmentId,
                Name = "IT",
                Description = "Information Technology Department",
                IsActive = true,
                CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Department
            {
                Id = HrDepartmentId,
                Name = "Human Resources",
                Description = "Human Resources Department",
                IsActive = true,
                CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Department
            {
                Id = FinanceDepartmentId,
                Name = "Finance",
                Description = "Finance & Accounting Department",
                IsActive = true,
                CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }
        );
    }

    private static void SeedEmployees(ModelBuilder modelBuilder)
    {
        // Passwords hashed with BCrypt — matching the seed accounts in README
        // Admin@123, Manager@123, Employee@123
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var managerHash = BCrypt.Net.BCrypt.HashPassword("Manager@123");
        var employeeHash = BCrypt.Net.BCrypt.HashPassword("Employee@123");

        modelBuilder.Entity<Employee>().HasData(
            new Employee
            {
                Id = AdminId,
                EmployeeCode = "EMP001",
                FirstName = "System",
                LastName = "Admin",
                Email = "admin@ems.com",
                PasswordHash = adminHash,
                PhoneNumber = "+94771234567",
                Role = UserRole.Admin,
                DepartmentId = ItDepartmentId,
                ManagerId = null,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Employee
            {
                Id = ManagerId,
                EmployeeCode = "EMP002",
                FirstName = "John",
                LastName = "Manager",
                Email = "manager@ems.com",
                PasswordHash = managerHash,
                PhoneNumber = "+94771234568",
                Role = UserRole.Manager,
                DepartmentId = ItDepartmentId,
                ManagerId = AdminId,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Employee
            {
                Id = EmployeeId,
                EmployeeCode = "EMP003",
                FirstName = "Jane",
                LastName = "Employee",
                Email = "employee@ems.com",
                PasswordHash = employeeHash,
                PhoneNumber = "+94771234569",
                Role = UserRole.Employee,
                DepartmentId = ItDepartmentId,
                ManagerId = ManagerId,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }
        );
    }
}
