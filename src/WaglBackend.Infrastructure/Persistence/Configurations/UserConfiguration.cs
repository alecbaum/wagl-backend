using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .HasMaxLength(50);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        // Configure TierLevel as owned type
        builder.OwnsOne(u => u.TierLevel, tierLevel =>
        {
            tierLevel.Property(tl => tl.Tier)
                .HasColumnName("TierLevel")
                .HasConversion<int>()
                .HasDefaultValue(AccountTier.Tier1);
        });

        // Configure relationships
        builder.HasMany(u => u.ApiUsageLogs)
            .WithOne(log => log.User)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.CreatedAt);
        builder.HasIndex(u => u.IsActive);
    }
}