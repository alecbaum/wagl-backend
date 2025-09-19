using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.RoomId)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.RoomId.From(v))
            .IsRequired();

        builder.Property(x => x.SessionId)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.SessionId.From(v))
            .IsRequired();

        builder.Property(x => x.ParticipantId)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.SentAt)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt);

        // UAI Integration Properties
        builder.Property(x => x.MessageType)
            .IsRequired()
            .HasDefaultValue(MessageType.UserMessage)
            .HasConversion<string>();

        builder.Property(x => x.ExternalMessageId)
            .HasMaxLength(255);

        builder.Property(x => x.TriggerMessageId);

        // Indexes
        builder.HasIndex(x => x.RoomId)
            .HasDatabaseName("IX_ChatMessages_RoomId");

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_ChatMessages_SessionId");

        builder.HasIndex(x => x.ParticipantId)
            .HasDatabaseName("IX_ChatMessages_ParticipantId");

        builder.HasIndex(x => x.SentAt)
            .HasDatabaseName("IX_ChatMessages_SentAt");

        builder.HasIndex(x => new { x.RoomId, x.SentAt })
            .HasDatabaseName("IX_ChatMessages_RoomId_SentAt");

        builder.HasIndex(x => new { x.SessionId, x.SentAt })
            .HasDatabaseName("IX_ChatMessages_SessionId_SentAt");

        builder.HasIndex(x => new { x.ParticipantId, x.SentAt })
            .HasDatabaseName("IX_ChatMessages_ParticipantId_SentAt");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("IX_ChatMessages_IsDeleted");

        // UAI Integration Indexes
        builder.HasIndex(x => x.MessageType)
            .HasDatabaseName("IX_ChatMessages_MessageType");

        builder.HasIndex(x => x.ExternalMessageId)
            .HasDatabaseName("IX_ChatMessages_ExternalMessageId");

        builder.HasIndex(x => x.TriggerMessageId)
            .HasDatabaseName("IX_ChatMessages_TriggerMessageId");

        // Foreign key relationships (explicit configuration for clarity)
        builder.HasOne<Participant>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}