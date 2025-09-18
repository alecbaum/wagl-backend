using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("ChatRooms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.RoomId.From(v))
            .IsRequired();

        builder.Property(x => x.SessionId)
            .HasConversion(
                v => v.Value,
                v => Core.Atoms.ValueObjects.SessionId.From(v))
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ParticipantCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MaxParticipants)
            .IsRequired()
            .HasDefaultValue(6);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<RoomStatus>(v))
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ClosedAt);

        // Computed properties
        builder.Ignore(x => x.IsFull);
        builder.Ignore(x => x.HasSpace);
        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.AvailableSlots);

        // Relationships
        builder.HasMany<Participant>()
            .WithOne()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<ChatMessage>()
            .WithOne()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_ChatRooms_SessionId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ChatRooms_Status");

        builder.HasIndex(x => new { x.SessionId, x.Status })
            .HasDatabaseName("IX_ChatRooms_SessionId_Status");

        builder.HasIndex(x => new { x.SessionId, x.ParticipantCount })
            .HasDatabaseName("IX_ChatRooms_SessionId_ParticipantCount");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_ChatRooms_CreatedAt");

        // Check constraints
        builder.HasCheckConstraint("CK_ChatRooms_ParticipantCount", "[ParticipantCount] >= 0");
        builder.HasCheckConstraint("CK_ChatRooms_MaxParticipants", "[MaxParticipants] > 0");
        builder.HasCheckConstraint("CK_ChatRooms_ParticipantCount_Limit", "[ParticipantCount] <= [MaxParticipants]");
    }
}