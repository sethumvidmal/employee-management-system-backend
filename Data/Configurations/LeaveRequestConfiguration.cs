using EmployeeManagement.Api.Entities;
using EmployeeManagement.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Api.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.LeaveType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Reason)
            .HasMaxLength(500);

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LeaveStatus.Pending);

        builder.Property(l => l.AppliedOn)
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => new { l.EmployeeId, l.Status });

        // Relationships
        builder.HasOne(l => l.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.ApprovedBy)
            .WithMany(e => e.ApprovedLeaveRequests)
            .HasForeignKey(l => l.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
