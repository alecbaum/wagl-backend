using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");

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

        builder.Property(x => x.UserId)
            .HasConversion(
                v => v != null ? v.Value : (Guid?)null,
                v => v.HasValue ? Core.Atoms.ValueObjects.UserId.From(v.Value) : null);

        // Create a shadow property for the foreign key to User
        builder.Property<Guid?>("UserGuidId");

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ConnectionId)
            .HasMaxLength(100);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ParticipantType>(v))
            .HasMaxLength(50);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.JoinedAt)
            .IsRequired();

        builder.Property(x => x.LeftAt);

        // Computed properties
        builder.Ignore(x => x.IsRegisteredUser);
        builder.Ignore(x => x.IsGuest);
        builder.Ignore(x => x.IsConnected);
        builder.Ignore(x => x.Duration);

        // Relationships
        builder.HasMany<ChatMessage>()
            .WithOne()
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to User (optional) - using shadow property
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey("UserGuidId")
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(x => x.RoomId)
            .HasDatabaseName("IX_Participants_RoomId");

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_Participants_SessionId");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Participants_UserId");

        builder.HasIndex(x => x.ConnectionId)
            .HasDatabaseName("IX_Participants_ConnectionId");

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("IX_Participants_Type");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Participants_IsActive");

        builder.HasIndex(x => new { x.RoomId, x.IsActive })
            .HasDatabaseName("IX_Participants_RoomId_IsActive");

        builder.HasIndex(x => new { x.SessionId, x.IsActive })
            .HasDatabaseName("IX_Participants_SessionId_IsActive");

        builder.HasIndex(x => new { x.UserId, x.SessionId })
            .HasDatabaseName("IX_Participants_UserId_SessionId");

        builder.HasIndex(x => x.JoinedAt)
            .HasDatabaseName("IX_Participants_JoinedAt");

        // Unique constraint: One active participant per user per session
        builder.HasIndex(x => new { x.UserId, x.SessionId, x.IsActive })
            .HasDatabaseName("IX_Participants_UserId_SessionId_IsActive")
            .HasFilter("[UserId] IS NOT NULL AND [IsActive] = 1");
    }
}