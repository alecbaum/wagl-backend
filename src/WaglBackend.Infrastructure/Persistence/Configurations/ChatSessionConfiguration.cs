using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("ChatSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.SessionId.From(v))
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Description property doesn't exist in ChatSession entity

        builder.Property(x => x.ScheduledStartTime)
            .IsRequired();

        builder.Property(x => x.Duration)
            .IsRequired();

        builder.Property(x => x.MaxParticipants)
            .IsRequired()
            .HasDefaultValue(36);

        builder.Property(x => x.MaxParticipantsPerRoom)
            .IsRequired()
            .HasDefaultValue(6);

        builder.Property(x => x.CreatedByUserId)
            .HasConversion(
                v => v != null ? v.Value : (Guid?)null,
                v => v.HasValue ? Core.Atoms.ValueObjects.UserId.From(v.Value) : null);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<SessionStatus>(v))
            .HasMaxLength(50);

        builder.Property(x => x.StartedAt);

        builder.Property(x => x.EndedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Computed properties
        builder.Ignore(x => x.ScheduledEndTime);
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.CanStart);
        builder.Ignore(x => x.IsActive);

        // UpdatedAt property doesn't exist in ChatSession entity

        // Relationships
        builder.HasMany<ChatRoom>()
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<SessionInvite>()
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<Participant>()
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<ChatMessage>()
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ChatSessions_Status");

        builder.HasIndex(x => x.ScheduledStartTime)
            .HasDatabaseName("IX_ChatSessions_ScheduledStartTime");

        builder.HasIndex(x => new { x.Status, x.ScheduledStartTime })
            .HasDatabaseName("IX_ChatSessions_Status_ScheduledStartTime");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_ChatSessions_CreatedAt");
    }
}