using EmployeeManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Api.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(d => d.Name)
            .IsUnique();
    }
}
