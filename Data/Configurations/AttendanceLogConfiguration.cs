using EmployeeManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Api.Data.Configurations;

public class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        builder.ToTable("AttendanceLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.DeviceType)
            .HasConversion<string>()
            .HasMaxLength(10);

        // Indexes
        builder.HasIndex(a => a.WorkDate);
        builder.HasIndex(a => new { a.EmployeeId, a.WorkDate });

        // Relationships
        builder.HasOne(a => a.Employee)
            .WithMany(e => e.AttendanceLogs)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
