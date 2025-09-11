using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class ApiUsageLogConfiguration : IEntityTypeConfiguration<ApiUsageLog>
{
    public void Configure(EntityTypeBuilder<ApiUsageLog> builder)
    {
        builder.ToTable("ApiUsageLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(log => log.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(log => log.IpAddress)
            .HasMaxLength(45); // Supports both IPv4 and IPv6

        builder.Property(log => log.UserAgent)
            .HasMaxLength(1000);

        builder.Property(log => log.RequestTimestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        builder.Property(log => log.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Configure relationships
        builder.HasOne(log => log.User)
            .WithMany(u => u.ApiUsageLogs)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(log => log.Provider)
            .WithMany(p => p.ApiUsageLogs)
            .HasForeignKey(log => log.ProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes for performance
        builder.HasIndex(log => log.RequestTimestamp);
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.ProviderId);
        builder.HasIndex(log => log.Endpoint);
        builder.HasIndex(log => log.AccountType);
        builder.HasIndex(log => log.ResponseStatusCode);

        // Composite indexes for common queries
        builder.HasIndex(log => new { log.UserId, log.RequestTimestamp });
        builder.HasIndex(log => new { log.ProviderId, log.RequestTimestamp });
        builder.HasIndex(log => new { log.Endpoint, log.RequestTimestamp });
        builder.HasIndex(log => new { log.AccountType, log.RequestTimestamp });
    }
}