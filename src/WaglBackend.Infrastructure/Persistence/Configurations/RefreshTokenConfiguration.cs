using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rt => rt.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(255);

        // Configure relationship
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes
        builder.HasIndex(rt => rt.Token)
            .IsUnique();

        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.ExpiresAt);
        builder.HasIndex(rt => rt.IsRevoked);
        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt });
    }
}