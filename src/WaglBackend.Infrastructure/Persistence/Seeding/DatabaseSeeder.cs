using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Seeding;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Run database migrations
            _logger.LogInformation("Running database migrations...");
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations completed successfully");

            // Seed roles
            await SeedRolesAsync();

            // Seed demo users (admin, tier users, moderators)
            await SeedDemoUsersAsync();

            // Seed demo provider for testing
            await SeedDemoProviderAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            "Tier1",
            "Tier2",
            "Tier3",
            "Provider",
            "Admin",
            "ChatModerator",
            "ChatAdmin"
        };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogWarning("Failed to create role {RoleName}: {Errors}", 
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedTierFeaturesAsync()
    {
        var features = new[]
        {
            // Tier 1 Features
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "BasicAPI",
                Description = "Access to basic API endpoints",
                RequiredTier = AccountTier.Tier1,
                FeatureFlag = FeatureFlags.BasicApi,
                IsEnabled = true,
                RateLimitPerHour = 100
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "StandardSupport",
                Description = "Standard email support",
                RequiredTier = AccountTier.Tier1,
                FeatureFlag = FeatureFlags.PrioritySupport,
                IsEnabled = true
            },

            // Tier 2 Features
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "AdvancedAPI",
                Description = "Access to advanced API features",
                RequiredTier = AccountTier.Tier2,
                FeatureFlag = FeatureFlags.AdvancedApi,
                IsEnabled = true,
                RateLimitPerHour = 500
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "Analytics",
                Description = "Usage analytics and reporting",
                RequiredTier = AccountTier.Tier2,
                FeatureFlag = FeatureFlags.Analytics,
                IsEnabled = true
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "Webhooks",
                Description = "Webhook notifications",
                RequiredTier = AccountTier.Tier2,
                FeatureFlag = FeatureFlags.Webhooks,
                IsEnabled = true
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "DataExport",
                Description = "Export data to various formats",
                RequiredTier = AccountTier.Tier2,
                FeatureFlag = FeatureFlags.DataExport,
                IsEnabled = true
            },

            // Tier 3 Features
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "PremiumAPI",
                Description = "Full premium API access",
                RequiredTier = AccountTier.Tier3,
                FeatureFlag = FeatureFlags.PremiumApi,
                IsEnabled = true,
                RateLimitPerHour = 2000
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "CustomIntegrations",
                Description = "Custom integration solutions",
                RequiredTier = AccountTier.Tier3,
                FeatureFlag = FeatureFlags.CustomIntegrations,
                IsEnabled = true
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "RealTimeNotifications",
                Description = "Real-time push notifications",
                RequiredTier = AccountTier.Tier3,
                FeatureFlag = FeatureFlags.RealTimeNotifications,
                IsEnabled = true
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "BulkOperations",
                Description = "Bulk data operations",
                RequiredTier = AccountTier.Tier3,
                FeatureFlag = FeatureFlags.BulkOperations,
                IsEnabled = true
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "TwentyFourSevenSupport",
                Description = "24/7 priority support",
                RequiredTier = AccountTier.Tier3,
                FeatureFlag = FeatureFlags.TwentyFourSevenSupport,
                IsEnabled = true
            },
            new TierFeature
            {
                Id = Guid.NewGuid(),
                FeatureName = "WhiteLabel",
                Description = "White-label solutions",
                RequiredTier = AccountTier.Tier3,
                FeatureFlag = FeatureFlags.WhiteLabel,
                IsEnabled = true
            }
        };

        foreach (var feature in features)
        {
            var existingFeature = await _context.TierFeatures
                .FirstOrDefaultAsync(tf => tf.FeatureName == feature.FeatureName && tf.RequiredTier == feature.RequiredTier);

            if (existingFeature == null)
            {
                _context.TierFeatures.Add(feature);
                _logger.LogInformation("Added tier feature: {FeatureName} for {Tier}", feature.FeatureName, feature.RequiredTier);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDemoUsersAsync()
    {
        _logger.LogInformation("Starting user seeding process...");

        var demoUsers = new[]
        {
            new { Email = "tier1@wagl.com", Password = "Tier1Pass123!", FirstName = "Tier1", LastName = "User", Tier = AccountTier.Tier1, Role = (string?)null },
            new { Email = "tier2@wagl.com", Password = "Tier2Pass123!", FirstName = "Tier2", LastName = "User", Tier = AccountTier.Tier2, Role = (string?)null },
            new { Email = "tier3@wagl.com", Password = "Tier3Pass123!", FirstName = "Tier3", LastName = "User", Tier = AccountTier.Tier3, Role = (string?)null },
            new { Email = "moderator@wagl.com", Password = "ModeratorPass123!", FirstName = "Moderator", LastName = "User", Tier = AccountTier.Tier2, Role = "ChatModerator" },
            new { Email = "admin@wagl.com", Password = "AdminPass123!", FirstName = "Admin", LastName = "User", Tier = AccountTier.Tier3, Role = "Admin" }
        };

        _logger.LogInformation("Attempting to seed {UserCount} demo users", demoUsers.Length);

        foreach (var demoUser in demoUsers)
        {
            try
            {
                _logger.LogInformation("Checking if user exists: {Email}", demoUser.Email);
                var existingUser = await _userManager.FindByEmailAsync(demoUser.Email);

                if (existingUser != null)
                {
                    _logger.LogInformation("User already exists, skipping: {Email}", existingUser.Email);
                    continue;
                }

                _logger.LogInformation("User does not exist, creating: {Email}", demoUser.Email);
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = demoUser.Email,
                    Email = demoUser.Email,
                    EmailConfirmed = true,
                    FirstName = demoUser.FirstName,
                    LastName = demoUser.LastName,
                    TierLevel = TierLevel.FromTier(demoUser.Tier),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, demoUser.Password);
                if (result.Succeeded)
                {
                    // Add tier role
                    await _userManager.AddToRoleAsync(user, demoUser.Tier.ToString());

                    // Add special role if specified
                    if (!string.IsNullOrEmpty(demoUser.Role))
                    {
                        await _userManager.AddToRoleAsync(user, demoUser.Role);
                    }

                    _logger.LogInformation("Created demo user: {Email} with tier {Tier} and role {Role}",
                        demoUser.Email, demoUser.Tier, demoUser.Role ?? "None");
                }
                else
                {
                    _logger.LogWarning("Failed to create demo user {Email}: {Errors}",
                        demoUser.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing demo user: {Email}", demoUser.Email);
            }
        }

        _logger.LogInformation("Completed user seeding process");
    }

    private async Task SeedDemoProvidersAsync()
    {
        var demoProviders = new[]
        {
            new { Name = "Demo Provider 1", ContactEmail = "provider1@example.com", Description = "First demo provider for testing" },
            new { Name = "Demo Provider 2", ContactEmail = "provider2@example.com", Description = "Second demo provider for testing" },
            new { Name = "Test Integration", ContactEmail = "integration@test.com", Description = "Test integration provider" }
        };

        foreach (var demoProvider in demoProviders)
        {
            var existingProvider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Name == demoProvider.Name);

            if (existingProvider == null)
            {
                var provider = new Provider
                {
                    Id = Guid.NewGuid(),
                    Name = demoProvider.Name,
                    ContactEmail = demoProvider.ContactEmail,
                    Description = demoProvider.Description,
                    ApiKey = ApiKey.Create(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    AllowedIpAddresses = new[] { "127.0.0.1", "::1", "localhost" }
                };

                _context.Providers.Add(provider);
                _logger.LogInformation("Created demo provider: {Name} with API key: {ApiKey}", 
                    provider.Name, provider.ApiKey?.Value ?? "NULL");
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDemoProviderAsync()
    {
        // Create a specific demo provider with a known API key for documentation purposes
        const string demoApiKeyValue = "wagl_DemoProvider2024ExampleKeyForTesting";
        var existingProvider = await _context.Providers
            .FirstOrDefaultAsync(p => p.Name == "Demo Provider");

        if (existingProvider == null)
        {
            var demoApiKey = ApiKey.FromString(demoApiKeyValue);
            
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                Name = "Demo Provider",
                ContactEmail = "demo@provider.com",
                Description = "Demo provider for testing API key authentication with 100M requests/hour limit",
                ApiKey = demoApiKey,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                AllowedIpAddresses = new[] { "127.0.0.1", "::1", "localhost", "0.0.0.0/0" }
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created demo provider: {Name} with API key: {ApiKey}", 
                provider.Name, demoApiKeyValue);
        }
        else
        {
            _logger.LogInformation("Demo provider already exists with API key ending in: ...{Suffix}", 
                existingProvider.ApiKey?.Value?.Substring(Math.Max(0, existingProvider.ApiKey.Value.Length - 10)) ?? "NULL");
        }
    }
}