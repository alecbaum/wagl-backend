using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class SessionInviteConfiguration : IEntityTypeConfiguration<SessionInvite>
{
    public void Configure(EntityTypeBuilder<SessionInvite> builder)
    {
        builder.ToTable("SessionInvites");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.SessionId)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.SessionId.From(v))
            .IsRequired();

        builder.Property(x => x.Token)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.InviteToken.From(v))
            .IsRequired()
            .HasMaxLength(100);

        // MaxUses and UsesRemaining properties don't exist - using single-use invites

        builder.Property(x => x.InviteeEmail)
            .HasMaxLength(256);

        builder.Property(x => x.InviteeName)
            .HasMaxLength(100);

        builder.Property(x => x.IsConsumed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ConsumedAt);

        builder.Property(x => x.ConsumedByUserId)
            .HasConversion(
                v => v != null ? v.Value : (Guid?)null,
                v => v.HasValue ? Core.Atoms.ValueObjects.UserId.From(v.Value) : null);

        builder.Property(x => x.ConsumedByName)
            .HasMaxLength(100);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        // IsActive is a computed property based on IsConsumed and IsExpired

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // LastUsedAt is replaced by ConsumedAt, UpdatedAt doesn't exist

        // Indexes
        builder.HasIndex(x => x.Token)
            .IsUnique()
            .HasDatabaseName("IX_SessionInvites_Token");

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_SessionInvites_SessionId");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_SessionInvites_ExpiresAt");

        builder.HasIndex(x => x.IsConsumed)
            .HasDatabaseName("IX_SessionInvites_IsConsumed");

        builder.HasIndex(x => new { x.SessionId, x.IsConsumed })
            .HasDatabaseName("IX_SessionInvites_SessionId_IsConsumed");

        builder.HasIndex(x => new { x.IsConsumed, x.ExpiresAt })
            .HasDatabaseName("IX_SessionInvites_IsConsumed_ExpiresAt");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_SessionInvites_CreatedAt");

        // Computed properties
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsValid);
        builder.Ignore(x => x.CanBeUsed);
    }
}