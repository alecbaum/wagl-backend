using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Molecules.Interfaces;
using WaglBackend.Infrastructure.Persistence.Configurations;

namespace WaglBackend.Infrastructure.Persistence.Context;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Provider> Providers { get; set; }
    public DbSet<ApiUsageLog> ApiUsageLogs { get; set; }
    public DbSet<TierFeature> TierFeatures { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ProviderConfiguration());
        modelBuilder.ApplyConfiguration(new ApiUsageLogConfiguration());
        modelBuilder.ApplyConfiguration(new TierFeatureConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

        // Configure Identity tables with custom names
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
        });

        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("Roles");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add audit information before saving
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAuditable && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is IAuditable auditableEntity)
            {
                var now = DateTime.UtcNow;
                
                if (entityEntry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAt = now;
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    auditableEntity.UpdatedAt = now;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

// Add RefreshToken entity for JWT refresh token management
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}