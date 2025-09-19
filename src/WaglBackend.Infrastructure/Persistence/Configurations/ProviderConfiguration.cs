using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.ContactEmail)
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        // Configure ApiKey as owned type
        builder.OwnsOne(p => p.ApiKey, apiKey =>
        {
            apiKey.Property(a => a.Value)
                .HasColumnName("ApiKeyValue")
                .HasMaxLength(255)
                .IsRequired(false);

            apiKey.Property(a => a.HashedValue)
                .HasColumnName("ApiKeyHash")
                .HasMaxLength(255)
                .IsRequired(false);

            apiKey.Property(a => a.CreatedAt)
                .HasColumnName("ApiKeyCreatedAt");

            apiKey.HasIndex(a => a.Value)
                .IsUnique()
                .HasFilter("\"ApiKeyValue\" IS NOT NULL");
        });

        // Configure AllowedIpAddresses as JSON column with proper conversion
        builder.Property(p => p.AllowedIpAddresses)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<string[]>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Configure relationships
        builder.HasMany(p => p.ApiUsageLogs)
            .WithOne(log => log.Provider)
            .HasForeignKey(log => log.ProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes
        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.LastAccessedAt);
    }
}