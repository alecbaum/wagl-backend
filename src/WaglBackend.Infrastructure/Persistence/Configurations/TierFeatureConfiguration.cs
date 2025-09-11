using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Infrastructure.Persistence.Configurations;

public class TierFeatureConfiguration : IEntityTypeConfiguration<TierFeature>
{
    public void Configure(EntityTypeBuilder<TierFeature> builder)
    {
        builder.ToTable("TierFeatures");

        builder.HasKey(tf => tf.Id);

        builder.Property(tf => tf.FeatureName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tf => tf.Description)
            .HasMaxLength(500);

        builder.Property(tf => tf.RequiredTier)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(tf => tf.FeatureFlag)
            .HasConversion<long>();

        builder.Property(tf => tf.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(tf => tf.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        // Add indexes
        builder.HasIndex(tf => tf.RequiredTier);
        builder.HasIndex(tf => tf.FeatureName);
        builder.HasIndex(tf => tf.IsEnabled);
        builder.HasIndex(tf => new { tf.RequiredTier, tf.IsEnabled });

        // Add unique constraint
        builder.HasIndex(tf => new { tf.FeatureName, tf.RequiredTier })
            .IsUnique();
    }
}