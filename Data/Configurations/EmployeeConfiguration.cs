using EmployeeManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Api.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.EmployeeCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.PasswordHash)
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(e => e.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(e => e.EmployeeCode).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();

        // Relationships
        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
