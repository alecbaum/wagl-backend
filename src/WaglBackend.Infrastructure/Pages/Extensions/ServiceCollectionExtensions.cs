using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Domain.Organisms.Services.Caching;
using WaglBackend.Infrastructure.Pages.Features.Authentication;
using WaglBackend.Infrastructure.Pages.Features.UserManagement;
using WaglBackend.Infrastructure.Pages.Features.ProviderManagement;
using WaglBackend.Infrastructure.Pages.Features.Analytics;
using WaglBackend.Infrastructure.Persistence.Context;
using WaglBackend.Infrastructure.Persistence.Seeding;
using WaglBackend.Infrastructure.Services.Authentication;
using WaglBackend.Infrastructure.Services.Caching;

namespace WaglBackend.Infrastructure.Pages.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtomicDesignServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register Molecules - Configuration objects
        services.Configure<JwtConfiguration>(
            configuration.GetSection(JwtConfiguration.SectionName));
        services.Configure<RedisConfiguration>(
            configuration.GetSection(RedisConfiguration.SectionName));
        services.Configure<RateLimitConfiguration>(
            configuration.GetSection(RateLimitConfiguration.SectionName));
        services.Configure<DatabaseConfiguration>(
            configuration.GetSection(DatabaseConfiguration.SectionName));

        // Register Entity Framework DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("WaglBackend.Infrastructure");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Configure for development/production
            if (configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            if (configuration.GetValue<bool>("Database:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }

            // Suppress pending model changes warning for development
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Register ASP.NET Core Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register Redis Cache
        var redisConfig = configuration.GetSection(RedisConfiguration.SectionName).Get<RedisConfiguration>();
        if (redisConfig != null && !string.IsNullOrEmpty(redisConfig.ConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfig.ConnectionString;
                options.InstanceName = redisConfig.InstanceName;
            });
        }
        else
        {
            // Fallback to in-memory cache if Redis is not configured
            services.AddMemoryCache();
        }

        // Register Organisms - Services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IUserAuthService, UserAuthService>();
        services.AddScoped<DatabaseSeeder>();
        // services.AddScoped<IUserService, UserService>();
        // services.AddScoped<IProviderService, ProviderService>();
        // services.AddScoped<ITierManagementService, TierManagementService>();
        // services.AddScoped<IRateLimitService, TierRateLimitService>();

        // Register Organisms - Repositories (would be implemented in actual repositories)
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IProviderRepository, ProviderRepository>();
        // services.AddScoped<IApiUsageRepository, ApiUsageRepository>();

        // Register Pages - Feature Modules
        services.RegisterModule<AuthenticationModule>(configuration);
        services.RegisterModule<UserManagementModule>(configuration);
        services.RegisterModule<ProviderManagementModule>(configuration);
        services.RegisterModule<AnalyticsModule>(configuration);

        return services;
    }

    public static IServiceCollection RegisterModule<TModule>(
        this IServiceCollection services, 
        IConfiguration configuration) where TModule : IModule, new()
    {
        var module = new TModule();
        module.RegisterServices(services, configuration);
        return services;
    }
}

public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}