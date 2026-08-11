using EmployeeManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Api.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(r => r.Token).IsUnique();
        builder.HasIndex(r => r.EmployeeId);

        // Relationships
        builder.HasOne(r => r.Employee)
            .WithMany(e => e.RefreshTokens)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsActive);
    }
}
